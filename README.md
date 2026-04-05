# MemoAI 🧠

**MemoAI** is a premium note-taking application for Windows that replaces traditional keyword search with **AI-powered semantic retrieval**. It understands the core meaning behind your notes, allowing you to find information based on context rather than exact character matches.

## 🚀 Key Concepts

### On-Device Semantic Search
Instead of basic text matching, this app leverages **Vector Embeddings**:
* **Model:** Uses `all-MiniLM-L6-v2` (ONNX format) to convert every note into a 384-dimensional mathematical vector.
* **Logic:** The app calculates the "closeness" (Cosine Similarity) between your search query and your notes. For example, searching for "healthy and good eating" can successfully find a note titled "Salad Recipes for launch."
* **Privacy:** All AI processing is done locally. No data ever leaves your machine or is sent to the cloud.

### Optimized Storage Architecture
The backend is designed for speed, portability, and a minimal footprint:
* **Database:** Powered by **SQLite**, removing the need for heavy external servers like PostgreSQL.
* **Vector Extension:** Integrates **`sqlite-vec`**, enabling high-performance k-nearest neighbor (KNN) vector search directly through standard SQL queries.
* **Efficiency:** Through dependency optimization and local model execution, the app size is approximately **100MB** (a massive reduction from the 600MB+ seen in standard implementations).

## 🛠️ Technical Specifications

* **Framework:** Built with **WPF (.NET)** for a native, fluid Windows experience.
* **AI Engine:** Utilizes `Microsoft.Extensions.AI` with local ONNX runtime execution.
* **Vector Storage:** Uses a virtual table (`vec0`) for instantaneous semantic indexing.

## 📂 Project Structure

* **`EmbeddingService.cs`:** The core engine managing the AI model lifecycle, SQLite connection, and vector operations.
* **`MainWindow.xaml`:** Modern WPF UI handling note management and real-time search updates.
* **`model/`:** Local directory containing the `model.onnx` embedding file.
* **`smartmemo.db`:** The local SQLite database file generated automatically on the first run.

## 🔧 Setup & Build

1. **Prerequisites:** Visual Studio with the ".NET Desktop Development" workload installed.
2. **Clone the Repo:** ```bash
   git clone https://github.com/zakariabouchelaghm/MemoAI-WPF-version
