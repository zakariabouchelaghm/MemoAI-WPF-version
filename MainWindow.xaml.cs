using MemoAI.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MemoAI
{
    public partial class MainWindow : Window
    {
        // Ton service qui contient maintenant le LocalEmbeddingGenerator
        private static readonly EmbeddingService _embeddingService = new EmbeddingService();
        private List<NoteItem> _allNotes = new();
        private bool _isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Chargement lourd : Base de données + Modèle IA (80MB)
                await _embeddingService.InitializeAsync();

                _allNotes = await _embeddingService.GetAllNotesAsync();
                lstNotes.ItemsSource = _allNotes;

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization Error : {ex.Message}");
            }
        }

        // --- GESTION DES NOTES ---

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var newNote = new NoteItem
            {
                id = -1,
                TitleText = "New Title",
                NoteText = "New Note",
                Date = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            };

            _allNotes.Insert(0, newNote);

            lstNotes.ItemsSource = null;
            lstNotes.ItemsSource = _allNotes;
            lstNotes.SelectedItem = newNote;

            titleEditor.Focus();
            titleEditor.SelectAll();
        }

        private async void save(object sender, RoutedEventArgs e)
        {
            if (lstNotes.SelectedItem is NoteItem selectedNote)
            {
                try
                {
                    selectedNote.TitleText = titleEditor.Text;
                    selectedNote.NoteText = noteEditor.Text;
                    //selectedNote.Date = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                    // Utilise l'IA pour générer le vecteur avant de sauver en SQLite
                    if (selectedNote.id == -1)
                    {
                        await _embeddingService.SaveNoteWithEmbedding(selectedNote.TitleText, selectedNote.NoteText, selectedNote.Date);
                        // Refresh pour récupérer l'ID réel
                        _allNotes = await _embeddingService.GetAllNotesAsync();
                        lstNotes.ItemsSource = _allNotes;
                    }
                    else
                    {
                        await _embeddingService.UpdateNoteContent(selectedNote);
                    }

                    lstNotes.Items.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Saving Error : {ex.Message}");
                }
            }
        }

        private async void Delete(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is NoteItem noteToDelete)
            {
                
                
                    try
                    {
                        await _embeddingService.DeleteNote(noteToDelete.id);
                        _allNotes.Remove(noteToDelete);

                        lstNotes.ItemsSource = null;
                        lstNotes.ItemsSource = _allNotes;
                        EditorGrid.Visibility = Visibility.Collapsed;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Suppression Error: {ex.Message}");
                    }
                
            }
        }

        // --- RECHERCHE IA (RAG) ---

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string[] words = txtSearch.Text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length<3 )
            {
                MessageBox.Show("Search sentence must be with 3 or more words.");
                return;
            };


            if (string.IsNullOrWhiteSpace(txtSearch.Text)) return;
            if (!_isInitialized) return;

            try
            {
                btnSearch.IsEnabled = false;
                // On cherche les notes les plus proches sémantiquement
                var foundNotes = await _embeddingService.SearchSimilarNotes(txtSearch.Text, limit: 3);

                lstNotes.ItemsSource = null;
                lstNotes.ItemsSource = foundNotes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Research Error : {ex.Message}");
            }
            finally
            {
                btnSearch.IsEnabled = true;
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                lstNotes.ItemsSource = _allNotes;
            }
        }

        // --- INTERFACE ET UX ---

        private void lstNotes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstNotes.SelectedItem is NoteItem selected)
            {
                EditorGrid.Visibility = Visibility.Visible;
                titleEditor.Text = selected.TitleText;
                noteEditor.Text = selected.NoteText;
                DateEditor.Text = selected.Date;
            }
        }

        private void noteEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized || notelength == null) return;

            if (sender is TextBox tb)
            {
                // Mise à jour du compteur (ex: 120/300)
                notelength.Text = $"{tb.Text.Length}/300";
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) this.DragMove();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        // Placeholders pour tes futurs besoins
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) { }
        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }
    }
}