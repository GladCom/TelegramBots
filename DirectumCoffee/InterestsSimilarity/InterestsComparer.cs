using Newtonsoft.Json;
using Python.Runtime;

namespace DirectumCoffee.InterestsSimilarity;

/// <summary>
/// Анализатор семантического сходства интересов.
/// </summary>
public class InterestsComparer
{
  #region Поля и свойства
  
  /// <summary>
  /// Путь до бибилиотеки python.dll.
  /// </summary>
  private readonly string pythonDllPath;
  
  /// <summary>
  /// Имя модели для сравнения слов или фраз.
  /// </summary>
  private readonly string modelName;
  
  #endregion
  
  #region Методы
  /// <summary>
  /// Сравнить две фразы на сходство.
  /// </summary>
  /// <param name="mainPhrase">Первая фраза.</param>
  /// <param name="phrasesToCompare">Фразы для сравнения.</param>
  /// <returns>Коэффициент сходства.</returns>
  public float[] Compare(string mainPhrase, string[] phrasesToCompare)
  {
    
    using (Py.GIL())
    {
      dynamic np = Py.Import("numpy");
      dynamic sentence_transformers = Py.Import("sentence_transformers");
      dynamic sklearn = Py.Import("sklearn.metrics.pairwise");
      
      dynamic model = sentence_transformers.SentenceTransformer(this.modelName);
      string[] allPhrases = new string[phrasesToCompare.Length + 1];
      allPhrases[0] = mainPhrase;
      Array.Copy(phrasesToCompare, 0, allPhrases, 1, phrasesToCompare.Length);
      
      dynamic embeddings = model.encode(allPhrases);
      dynamic mainEmbedding = embeddings[0];
      
      float[] similarities = new float[phrasesToCompare.Length];
      for (int i = 0; i < phrasesToCompare.Length; i++)
      {
        dynamic currentEmbedding = embeddings[i + 1];
        dynamic similarity = sklearn.cosine_similarity(
          mainEmbedding.reshape(1, -1),
          currentEmbedding.reshape(1, -1))[0][0];

        similarities[i] = (float)similarity;
      }

      return similarities;
    }
  }
  
  /// <summary>
  /// Прочитать конфиг.
  /// </summary>
  /// <returns></returns>
  /// <exception cref="FileNotFoundException">Возвращается если файл не найден.</exception>
  /// <exception cref="ApplicationException">Возвращается если произошли ошибки при чтении конфига.</exception>
  private PythonConfig LoadConfig()
  {
    try
    {
      string configPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "InterestsSimilarity\\python_config.json");
      if (!File.Exists(configPath))
        throw new FileNotFoundException($"Configuration file not found at {configPath}");
        
      string json = File.ReadAllText(configPath);
      return JsonConvert.DeserializeObject<PythonConfig>(json);
    }
    catch (Exception ex)
    {
      throw new ApplicationException("Failed to load configuration", ex);
    }
  }
  
  #endregion

  #region Конструкторы
  
  /// <summary>
  /// Конструктор.
  /// </summary>
  public InterestsComparer()
  {
    var config = LoadConfig();
    this.pythonDllPath = config.PythonDllPath;
    this.modelName = config.ModelName;
    Runtime.PythonDLL = this.pythonDllPath;
    PythonEngine.Initialize();    
  }
  
  #endregion
}