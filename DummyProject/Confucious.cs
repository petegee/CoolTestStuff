
namespace DummyProject
{
    public class Confucious
    {
        public Confucious(IQuoteGenerator philosophicalQuoteGenerator, IQuoteGenerator movieQuoteGenerator)
        {
            PhilosophicalQuoteGenerator = philosophicalQuoteGenerator;
            MovieQuoteGenerator = movieQuoteGenerator;
        }

        public IQuoteGenerator PhilosophicalQuoteGenerator { get; set; }

        public IQuoteGenerator MovieQuoteGenerator { get; set; }

        public string ImpartWiseWordsOfWisdom()
        {
            return $"Confuciuos says: {GetTheQuoutes()}";
        }

        public virtual string GetTheQuoutes()
        {
            return $"(real GetQuote()): Philospher says: {PhilosophicalQuoteGenerator.SaySomething()}\nFavourite character says: {MovieQuoteGenerator.SaySomething()}";
        }
    }



}
