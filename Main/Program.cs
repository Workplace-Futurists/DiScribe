namespace DiScribe.Main
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            /* this function takes an boolean value as parameter
             * which by default is false
             * specifies whether this project is
             * being run with release
             * as release mode has different file pathing
             */
            Executor.Execute();
        }
    }
}
