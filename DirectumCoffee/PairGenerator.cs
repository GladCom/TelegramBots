using edu.stanford.nlp.ling;
using edu.stanford.nlp.pipeline;
using edu.stanford.nlp.util;
using java.util;

namespace DirectumCoffee
{
    using InterestsSimilarity;

    public class PairGenerator
    {
        [Obsolete]
        private StanfordCoreNLP pipeline;
        
        /// <summary>
        /// Анализатор сходства интересов.
        /// </summary>
        private readonly InterestsComparer interestsComparer = new ();
        
        /// <summary>
        /// Список пользователей у которых есть пары.
        /// </summary>
        private HashSet<long> pairedUsers = new HashSet<long>();

        public PairGenerator()
        {
            Properties props = new Properties();
            props.setProperty("annotators", "tokenize, ssplit, pos, lemma, ner, parse, sentiment");
            props.setProperty("ner.useSUTime", "false");
            this.pipeline = new StanfordCoreNLP(props);
        }

        public void GeneratePairs(Dictionary<long, string> profiles)
        {
            List<UserProfile> userProfiles = profiles
                .Select(p => new UserProfile
                {
                    Id = p.Key,
                    Interests = p.Value
                })
                .ToList();
            foreach (UserProfile userProfile in userProfiles)
            {
                if (this.pairedUsers.Contains(userProfile.Id))
                    continue;
                userProfiles.Remove(userProfile);
                this.GenerateOnePair(userProfile, userProfiles);
                userProfiles.Add(userProfile);
            }
            
            
            for (int i = 0; i < profiles.Count - 1; i++)
            {
                var profile1 = profiles.ElementAt(i);
                if (this.pairedUsers.Contains(profile1.Key))
                    continue;
                var interestsSimilarity = this.interestsComparer.Compare(profile1.Value, profiles.Values.ToArray());
                var mostSimilarityProfile = Array.IndexOf(interestsSimilarity, interestsSimilarity.Max());
            
                long bestMatchUserId = 0;
                int maxCommonKeywords = 0;
                string[] commonInterests = null;
            
                for (int j = i + 1; j < profiles.Count; j++)
                {
                    var profile2 = profiles.ElementAt(j);
                    
                    var isPairCreatedEarlier = BotDbContext.Instance.CoffeePairs
                        .Any(p => (p.FirstUserId == profile1.Key && p.SecondUserId == profile2.Key && p.PairingDate != DateTime.Today) 
                            || (p.FirstUserId == profile2.Key && p.SecondUserId == profile1.Key && p.PairingDate != DateTime.Today));
    
                    if (pairedUsers.Contains(profile2.Key) || isPairCreatedEarlier)
                        continue;
                }
            
                var pair = new CoffeePair
                {
                    FirstUserId = profile1.Key,
                    SecondUserId = bestMatchUserId != 0 ? bestMatchUserId : -1,
                    CommonInterests = commonInterests ?? Array.Empty<string>(),
                    PairingDate = DateTime.Today
                };
        
                BotDbContext.Instance.CoffeePairs.Add(pair);
                pairedUsers.Add(profile1.Key);
                if (bestMatchUserId != 0)
                    pairedUsers.Add(bestMatchUserId);
            }
            
            BotDbContext.Instance.SaveChanges();
        }

        private long GenerateOnePair(UserProfile targetProfile, List<UserProfile> profilesForMatch)
        {
            var interests = profilesForMatch.Select(p => p.Interests).ToArray();
            var interestsSimilarity = this.interestsComparer.Compare(targetProfile.Interests, interests);
            return this.GetPair(targetProfile, interestsSimilarity);
            
            var mostSimilarityProfileIdInArray = Array.IndexOf(interestsSimilarity, interestsSimilarity.Max());
            var isPairCreatedEarlier = BotDbContext.Instance.CoffeePairs
                .Any(p => (p.FirstUserId == targetProfile.Id && p.SecondUserId == profilesForMatch[mostSimilarityProfileIdInArray].Id
                                                             && p.PairingDate != DateTime.Today) 
                          || (p.FirstUserId == profilesForMatch[mostSimilarityProfileIdInArray].Id &&
                              p.SecondUserId == targetProfile.Id && p.PairingDate != DateTime.Today));
    
            if (this.pairedUsers.Contains(profilesForMatch[mostSimilarityProfileIdInArray].Id) || isPairCreatedEarlier)
                continue;
            return profilesForMatch[mostSimilarityProfileIdInArray].Id;
        }

        private void GetPair(UserProfile targetProfile, float[] interestsSimilarity,List<UserProfile> profilesForMatch)
        {
            var mostSimilarityProfileIdInArray = Array.IndexOf(interestsSimilarity, interestsSimilarity.Max());
            var isPairCreatedEarlier = BotDbContext.Instance.CoffeePairs
                .Any(p => (p.FirstUserId == targetProfile.Id && p.SecondUserId == profilesForMatch[mostSimilarityProfileIdInArray].Id
                                                             && p.PairingDate != DateTime.Today) 
                          || (p.FirstUserId == profilesForMatch[mostSimilarityProfileIdInArray].Id &&
                              p.SecondUserId == targetProfile.Id && p.PairingDate != DateTime.Today));
            if (isPairCreatedEarlier)
            {
                if (interestsSimilarity.Length == 0)
                    return null;
                this.GetPair(targetProfile, interestsSimilarity);
            }
            else return ;
        }

        private List<string> ExtractKeywords(Annotation annotation)
        {
            string punctuationAndSymbols = ",.!?:;…-—'\"“”‘’!?\t\n\r/\\@#$%&*+-=<>()[]{}";
            string[] insignificantWords = {
                "а", "и", "или", "но", "да", "также", "тоже",
                "как", "что", "чтобы", "если", "когда", "пока", "потому", "чем",
                "с", "от", "до", "в", "на", "у", "о", "из", "перед", "под", "за",
                "при", "без", "по", "над", "после", "про", "между", "для", "во",
                "со", "к", "об", "поэтому", "таким образом", "следовательно",
                "тем не менее", "однако", "всё равно", "всё же", "так и", "то есть",
                "например", "кстати", "возможно", "может быть", "бывает",
                "как бы", "впрочем", "впрочём", "хотя", "даже", "только", "лишь",
                "вот", "всего", "чтож", "ну", "тут", "там", "здесь", "туда", "сюда", "тогда",
                "потом", "ибо", "ещё", "всегда", "всюду", "просто", "несмотря на", "причём"
            };

            List<string> keywords = new List<string>();

            var sentences = annotation.get(new CoreAnnotations.SentencesAnnotation().getClass()) as ArrayList;
            if (sentences != null)
            {
                foreach (CoreMap sentence in sentences)
                {
                    var words = sentence.get(new CoreAnnotations.TokensAnnotation().getClass()) as ArrayList;
                    if (words != null)
                    {
                        foreach (CoreLabel word in words)
                        {
                            string lemma = word.getString(new CoreAnnotations.LemmaAnnotation().getClass());
                            if (!string.IsNullOrWhiteSpace(lemma) && 
                                !punctuationAndSymbols.Contains(lemma) &&
                                !insignificantWords.Contains(lemma))
                            {
                                keywords.Add(lemma.ToLower());
                            }
                        }
                    }
                }
            }

            return keywords;
        }
    }
}
