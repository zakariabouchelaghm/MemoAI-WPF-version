using ElBruno.LocalEmbeddings;
using ElBruno.LocalEmbeddings.Options;
using Microsoft.Extensions.AI; // Souvent nécessaire pour IEmbeddingGenerator
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Threading.Tasks;

public class SearchService
{
    // Utilisation du nouveau nom de classe
    private LocalEmbeddingGenerator _generator;
    public bool IsInitialized { get; private set; } = false;

    // Vérifiez que ce using est présent


    public void LoadLocalModel()
    {
        string appPath = AppDomain.CurrentDomain.BaseDirectory;
        string modelPath = Path.Combine(appPath, "model");

        if (Directory.Exists(modelPath))
        {
            // On ne garde que la propriété indispensable : le chemin du modèle
            var options = new LocalEmbeddingsOptions
            {
                ModelPath = modelPath
            };

            // Initialisation du générateur avec l'objet options
            _generator = new LocalEmbeddingGenerator(options);

            IsInitialized = true;
        }
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (!IsInitialized) return null;

        // result est de type Embedding<float>
        var result = await _generator.GenerateEmbeddingAsync(text);

        // On extrait le vecteur (ReadOnlyMemory<float>) et on le convertit en tableau
        return result.Vector.ToArray();
    }
}