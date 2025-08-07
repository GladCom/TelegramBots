using Python.Runtime;

namespace DirectumCoffee.InterestsSimilarity;

public class InterestsComparer
{
  public dynamic CompareInterests(string topic1, string topic2)
  {
    Runtime.PythonDLL = @"C:\Users\Зверь\AppData\Local\Programs\Python\Python39\python39.dll";
    PythonEngine.Initialize();
    using (Py.GIL()) // Acquire the Python GIL
    {
      dynamic np = Py.Import("numpy");
      dynamic sentence_transformers = Py.Import("sentence_transformers");
      dynamic sklearn = Py.Import("sklearn.metrics.pairwise");

      // Создаем модель
      dynamic model = sentence_transformers.SentenceTransformer("all-mpnet-base-v2");

      // Получаем эмбеддинги
      dynamic embeddings = model.encode(new[] { topic1, topic2});

      // Вычисляем схожесть
      dynamic similarity_ai_ml = sklearn.cosine_similarity(
        embeddings[0].reshape(1, -1),
        embeddings[1].reshape(1, -1))[0][0];

      // Аналогично для других сравнений...

      return similarity_ai_ml;
    }
  }
  
  public InterestsComparer()
  {
    //Runtime.PythonDLL = @"C:\Users\Зверь\AppData\Local\Programs\Python\Python39\python39.dll";
  }
}