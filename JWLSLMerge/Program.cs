namespace JWLSLMerge
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Obtém todos os arquivos .jwlibrary na pasta do executável
            string[] jwlibraryFiles = null;

            if (args.Length == 0)
            {
                // Se nenhum argumento for fornecido, obtém todos os arquivos .jwlibrary na pasta do executável
                jwlibraryFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.jwlibrary");
            }
            else
            {
                switch (args[0].ToLower().Trim())
                {
                    case "-help":
                        // Mostra informações de ajuda sobre como usar o programa
                        ShowHelp();
                        return;

                    case "-folder":
                    case "-files":
                        // Obtém os arquivos .jwlibrary com base nos argumentos fornecidos
                        jwlibraryFiles = GetFiles(args);
                        break;

                    default:
                        break;
                }
            }

            // Verifica se há pelo menos dois arquivos .jwlibrary para mesclar
            if (jwlibraryFiles == null || jwlibraryFiles.Length < 2)
            {
                // Se não houver arquivos suficientes, exibe uma mensagem de erro
                Console.WriteLine("Please make sure there are at least two .jwlibrary files in the executable folder.");
                Console.WriteLine("Type -help for more information.");
                return;
            }

            // Inicializa o serviço de mesclagem e define um manipulador de eventos para mensagens
            MergeService mergeService = new MergeService();
            mergeService.Message += MergeService_Message;
            mergeService.Run(jwlibraryFiles);
        }

        // Obtém a lista de arquivos .jwlibrary com base nos argumentos fornecidos
        private static string[] GetFiles(string[] args)
        {
            if (args.Length == 1)
                Console.WriteLine("Invalid arguments. Type -help for more information.");

            if (args[0].ToLower().Equals("-folder"))
            {
                // Retorna os arquivos .jwlibrary da pasta especificada
                return Directory.GetFiles(args[1], "*.jwlibrary");
            }
            else
            {
                // Retorna a lista de arquivos .jwlibrary fornecida como argumentos
                return args
                    .Skip(1)
                    .Where(p => File.Exists(p) && Path.GetExtension(p.ToLower()) == ".jwlibrary")
                    .ToArray();
            }
        }

        // Exibe informações de ajuda sobre como usar o programa
        private static void ShowHelp()
        {
            Console.WriteLine("To merge all the .jwlibrary files, just place them in this same directory and run the command: JWLSLMerge.exe");
            Console.WriteLine("");
            Console.WriteLine("If you wish, you can define the location of the files through the -folder parameter followed by the directory where the files are located.");
            Console.WriteLine("Example: JWLSLMerge.exe -folder \"c:\\my backups\"");
            Console.WriteLine("");
            Console.WriteLine("If you want to specify the files you want to merge, use the -files parameter followed by the full path of each file.");
            Console.WriteLine("Example: JWLSLMerge.exe -files \"c:\\my backups\\theme_003.jwlibrary\" \"c:\\my backups\\theme_157.jwlibrary\"");
        }

        // Manipulador de eventos para exibir mensagens do serviço de mesclagem
        private static void MergeService_Message(object? sender, string e)
        {
            Console.WriteLine(e);
        }
    }
}
