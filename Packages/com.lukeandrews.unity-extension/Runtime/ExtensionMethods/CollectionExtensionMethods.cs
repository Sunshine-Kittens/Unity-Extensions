using System.Collections;

namespace UnityEngine.Extension
{
    public static class CollectionExtensionMethods
    {
        public static bool IsValidIndex<T>(this T collection, int index) where T : class, IList, ICollection
        {
            if (collection.Count > 0)
            {
                if (index >= 0 && index < collection.Count)
                {
                    return true;
                }
            }
            return false;
        }

        public static int DecrementIndex<T>(this T collection, int index, int amount) where T : class, IList, ICollection
        {
            int newId = index - amount;
            if (newId < 0)
            {
                newId = Mathf.Abs(newId);
                if (newId > collection.Count)
                {
                    newId = newId % collection.Count;
                }
                newId = (collection.Count - newId) % collection.Count;
            }
            return newId;
        }

        public static int IncrementIndex<T>(this T collection, int index, int amount) where T : class, IList, ICollection
        {
            return (index + amount) % collection.Count;
        }

        public static int DecrementIndex(int index, int amount, int length)
        {
            int newId = index - amount;
            if (newId < 0)
            {
                newId = Mathf.Abs(newId);
                if (newId > length)
                {
                    newId = newId % length;
                }
                newId = (length - newId) % length;
            }
            return newId;
        }

        public static int IncrementIndex(int index, int amount, int length)
        {
            return (index + amount) % length;
        }

        public static void Shuffle<T>(this T collection) where T : class, IList, ICollection
        {
            System.Random rng = new System.Random();
            int n = collection.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                object temp = collection[n];
                collection[n] = collection[k];
                collection[k] = temp;
            }
        }
    }
}
