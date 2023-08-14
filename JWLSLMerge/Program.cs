using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace JWLSLMerge
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Obtém a lista de arquivos .jwlibrary com base nos argumentos fornecidos
            string[] jwlibraryFiles = GetJWLibraryFiles(args);

            // Verifica se há pelo menos dois arquivos .jwlibrary para mesclar
            if (jwlibraryFiles.Length < 2)
            {
                Console.WriteLine("Please ensure there are at least two .jwlibrary files available.");
                Console.WriteLine("Type -help for more information.");
                Environment.ExitCode = 1; // Define o código de saída como 1 (erro)
                return;
            }

            // Inicializa o processo de mesclagem
            RunMergeService(jwlibraryFiles);
        }

        // Obtém a lista de arquivos .jwlibrary com base nos argumentos fornecidos
        private static string[] GetJWLibraryFiles(string[] args)
        {
            if (args.Length == 0)
            {
                // Se nenhum argumento for fornecido, obtém os arquivos .jwlibrary do diretório atual
                return GetFilesInCurrentDirectory("*.jwlibrary");
            }
            else
            {
                string option = args[0].ToLower().Trim();

                switch (option)
                {
                    case "-help":
                        ShowHelp();
                        Environment.ExitCode = 0; // Define o código de saída como 0 (sucesso)
                        break;

                    case "-folder":
                    case "-files":
                        return GetFiles(args.Skip(1).ToArray());

                    default:
                        Console.WriteLine("Invalid arguments. Type -help for more information.");
                        Environment.ExitCode = 1; // Define o código de saída como 1 (erro)
                        break;
                }
            }

            return Array.Empty<string>();
        }

        // Obtém a lista de arquivos .jwlibrary no diretório atual com base no padrão de busca
        private static string[] GetFilesInCurrentDirectory(string searchPattern)
        {
            return Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, searchPattern);
        }

        // Obtém a lista de arquivos .jwlibrary com base nos caminhos fornecidos
        private static string[] GetFiles(string[] paths)
        {
            List<string> validFiles = new List<string>();

            foreach (string path in paths)
            {
                if (File.Exists(path) && Path.GetExtension(path.ToLower()) == ".jwlibrary")
                {
                    validFiles.Add(path);
                }
                else
                {
                    Console.WriteLine($"Invalid file path: {path}");
                    Environment.ExitCode = 1; // Define o código de saída como 1 (erro)
                }
            }

            return validFiles.ToArray();
        }

        // Inicializa o processo de mesclagem com os arquivos fornecidos
        private static void RunMergeService(string[] files)
        {
            MergeService mergeService = new MergeService();
            mergeService.Message += MergeService_Message;
            mergeService.Run(files);
        }

        // Exibe informações de ajuda sobre como usar o programa
        private static void ShowHelp()
        {
            Console.WriteLine("JWLSLMerge - JW Library Files Merger");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("  JWLSLMerge.exe");
            Console.WriteLine("  JWLSLMerge.exe -folder <folder_path>");
            Console.WriteLine("  JWLSLMerge.exe -files <file_path> [<file_path> ...]");
            Console.WriteLine("");
            Console.WriteLine("Description:");
            Console.WriteLine("  JWLSLMerge is a tool for merging JW Library files (.jwlibrary) for backup or sharing.");
        }

        // Manipulador de eventos para exibir mensagens do serviço de mesclagem
        private static void MergeService_Message(object? sender, string e)
        {
            Console.WriteLine(e);
        }
    }
}
