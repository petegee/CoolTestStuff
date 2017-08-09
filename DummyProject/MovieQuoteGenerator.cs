namespace DummyProject
{
    public class MovieQuoteGenerator : IQuoteGenerator
    {
        private IImdb imdb;
        public MovieQuoteGenerator(IImdb imdb)
        {
            this.imdb = imdb;
        }

        public string SaySomething()
        {
            return imdb.GetTopMovieQuote();
        }  
    }
}
