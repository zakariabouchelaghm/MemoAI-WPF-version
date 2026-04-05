using System;
using System.Collections.Generic;
using System.Text;

namespace MemoAI.Models
{
    public class NoteItem
    {  
        public int id { get; set; }
        public string TitleText { get; set; }

        // The full content of the note
        public string NoteText { get; set; }
        public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string PreviewText
        {
            get
            {
                if (string.IsNullOrEmpty(NoteText)) return "Empty note...";
                return NoteText.Length <= 60
                    ? NoteText
                    : NoteText.Substring(0, 60).Replace(Environment.NewLine, " ") + "...";
            }
        }
    }
}
