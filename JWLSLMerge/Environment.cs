using System;
using System.IO;

namespace JWLSLMerge
{
    /// <summary>
    /// Fornece métodos de utilidade relacionados ao ambiente e diretórios da aplicação.
    /// </summary>
    public class Environment
    {
        /// <summary>
        /// Obtém o caminho para o diretório da aplicação.
        /// </summary>
        public static String ApplicationPath
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        /// <summary>
        /// Obtém ou define o código de saída da aplicação.
        /// </summary>
        public static int ExitCode { get; internal set; }

        /// <summary>
        /// Obtém o caminho do arquivo de banco de dados e o copia para o diretório de destino.
        /// </summary>
        /// <returns>O caminho para o arquivo de banco de dados copiado.</returns>
        public static string GetDbFile()
        {
            string dboriginal = Path.Combine(Environment.ApplicationPath, "DB", "userData.db");
            string dbcopy = Path.Combine(GetTargetDirectory(false), Path.GetFileName(dboriginal));

            if (File.Exists(dboriginal))
            {
                File.Copy(dboriginal, dbcopy, true);
            }

            return dbcopy;
        }

        /// <summary>
        /// Obtém o caminho do diretório temporário e opcionalmente o recria.
        /// </summary>
        /// <param name="recreate">Indica se deve recriar o diretório.</param>
        /// <returns>O caminho para o diretório temporário.</returns>
        public static string GetTempDirectory(bool recreate = true)
        {
            return GetDirectory("temp", recreate);
        }

        /// <summary>
        /// Obtém o caminho do diretório de destino e opcionalmente o recria.
        /// </summary>
        /// <param name="recreate">Indica se deve recriar o diretório.</param>
        /// <returns>O caminho para o diretório de destino.</returns>
        public static string GetTargetDirectory(bool recreate = true)
        {
            return GetDirectory("target", recreate);
        }

        /// <summary>
        /// Obtém o caminho do diretório mesclado.
        /// </summary>
        /// <returns>O caminho para o diretório mesclado.</returns>
        public static string GetMergedDirectory()
        {
            return GetDirectory("merged", true);
        }

        /// <summary>
        /// Obtém o caminho de um diretório especificado e opcionalmente o recria.
        /// </summary>
        /// <param name="folderName">O nome do diretório.</param>
        /// <param name="recreate">Indica se deve recriar o diretório.</param>
        /// <returns>O caminho para o diretório especificado.</returns>
        public static string GetDirectory(string folderName, bool recreate = true)
        {
            string tempdir = Path.Combine(Environment.ApplicationPath, folderName);

            if (recreate)
            {
                if (Directory.Exists(tempdir))
                {
                    Directory.Delete(tempdir, true);
                }

                Directory.CreateDirectory(tempdir);
            }

            return tempdir;
        }
    }
}
