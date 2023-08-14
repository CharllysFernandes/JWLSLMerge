using JWLSLMerge.Data;
using JWLSLMerge.Data.Models;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Security.Cryptography;

namespace JWLSLMerge
{
    public class MergeService
    {
        public event EventHandler<string>? Message;
        private readonly string targetPath = null!;
        private readonly string targetDbFile = null!;
        private string lastModified = null!;

        /// <summary>
        /// Inicializa uma nova instância da classe MergeService.
        /// </summary>
        public MergeService()
        {
            // Obter o diretório de destino a partir do ambiente.
            targetPath = Environment.GetTargetDirectory();

            // Obter o caminho para o arquivo de banco de dados de destino a partir do ambiente.
            targetDbFile = Environment.GetDbFile();
        }


        /// <summary>
        /// Executa o processo de mesclagem de arquivos do JW Library e criação do backup.
        /// </summary>
        /// <param name="jwlibraryFiles">Array de caminhos para arquivos do JW Library.</param>
        public void Run(string[] jwlibraryFiles)
        {
            try
            {
                // Informar que o processo de preparação do arquivo de banco de dados está ocorrendo.
                sendMessage("Preparing database file.");

                // Criar uma instância de JWDal para o arquivo de banco de dados de destino.
                JWDal dbMerged = new JWDal(targetDbFile);

                // Iterar sobre os arquivos do JW Library fornecidos.
                foreach (string file in jwlibraryFiles.Where(p => File.Exists(p)))
                {
                    // Informar sobre a leitura do arquivo atual.
                    sendMessage($"Reading {Path.GetFileName(file)} file.");

                    // Descompactar o arquivo para um diretório temporário.
                    string tempDir = Environment.GetTempDirectory();
                    ZipFile.ExtractToDirectory(file, tempDir, true);

                    // Procurar o arquivo "userData.db" no diretório temporário.
                    string? dbFile = Directory.GetFiles(tempDir, "userData.db").FirstOrDefault();

                    if (!string.IsNullOrEmpty(dbFile))
                    {
                        // Criar uma instância de JWDal para o banco de dados de origem.
                        JWDal dbSource = new JWDal(dbFile);

                        // Realizar a mesclagem dos bancos de dados.
                        merge(dbMerged, dbSource);

                        // Excluir o arquivo de banco de dados de origem.
                        File.Delete(dbFile);
                    }

                    // Copiar os arquivos de origem para o diretório de destino.
                    string[] files = Directory.GetFiles(tempDir, "*.*");

                    foreach (var item in files)
                    {
                        File.Move(item, Path.Combine(targetPath, Path.GetFileName(item)), true);
                    }
                }

                // Definir a data da última modificação no banco de dados mesclado.
                lastModified = dbMerged.SetLastModification();

                // Criar o arquivo manifest.json.
                createManifestFile();

                // Criar o arquivo JW Library.
                createJWLibraryFile();
            }
            catch (Exception ex)
            {
                // Informar em caso de erro durante o processo.
                sendMessage($"An error occurred while processing. Detail: {ex.Message}");
            }
        }

        private void createJWLibraryFile()
        {
            //create a jwlibrary file
            sendMessage($"Creating jwlibrary file.");

            string jwFile = Path.Combine(Environment.GetMergedDirectory(), "merged.jwlibrary");
            if (File.Exists(jwFile)) File.Delete(jwFile);

            ZipFile.CreateFromDirectory(targetPath, jwFile);

            sendMessage($"Done. The file has been created in {jwFile}.");
        }

        /// <summary>
        /// Cria um arquivo manifest.json contendo informações sobre a criação do arquivo de backup.
        /// </summary>
        private void createManifestFile()
        {
            // Enviar uma mensagem para informar que o arquivo manifest está sendo criado.
            sendMessage($"Creating manifest file.");

            // Criar um objeto Manifest com informações relevantes.
            Manifest manifest = new Manifest()
            {
                CreationDate = lastModified,
                Name = $"JWSLMerge_{lastModified}",
                UserDataBackup = new UserDataBackup()
                {
                    DatabaseName = "userData.db",
                    DeviceName = System.Environment.MachineName,
                    LastModifiedDate = lastModified,
                    SchemaVersion = 11,
                    Hash = GenerateDatabaseHash(targetDbFile)
                }
            };

            // Serializar o objeto Manifest para formato JSON.
            string jsonManifest = JsonConvert.SerializeObject(manifest, Newtonsoft.Json.Formatting.None);

            // Caminho completo para o arquivo manifest.json no diretório de destino.
            string pathManifest = Path.Combine(targetPath, "manifest.json");

            // Se o arquivo manifest.json já existir, exclua-o.
            if (File.Exists(pathManifest))
            {
                File.Delete(pathManifest);
            }

            // Escrever o JSON serializado no arquivo manifest.json.
            using (var sw = new StreamWriter(pathManifest, false))
            {
                sw.Write(jsonManifest);
            };
        }

        /// <summary>
        /// Envia uma mensagem através do evento <see cref="Message"/> se houver assinantes.
        /// </summary>
        /// <param name="message">A mensagem a ser enviada.</param>
        private void sendMessage(string message)
        {
            // Verifica se há assinantes para o evento Message
            if (Message != null)
            {
                // Chama todos os métodos de manipulador registrados para o evento Message,
                // passando 'this' como o remetente e a mensagem como argumento.
                Message(this, message);
            }
        }

        private void merge(JWDal dbMerged, JWDal dbSource)
        {
            /* Location
             * InputField (depend on Location)
             * BookMark (depend on Location)
             * UserMark (depend on Location)
             * BlockRange (depend on UserMark)
             * Note (depends on Location and UserMark)
             * IndependentMedia
             * PlayListItem (depends on IndependentMedia)
             * PlayListItemIndependentMediaMap (depends on IndependentMedia and PlayListItem)
             * PlayListItemLocationMap (depends on Location and PlayListItem)
             * Tag
             * TagMap (depends on Tag, Location, PlaylistItem and Note)
             * PlaylistItemMarker (depends on PlayListItem)
             * PlaylistItemMarkerBibleVerseMap (depends on PlaylistItemMarker)
             * PlaylistItemMarkerParagraphMap (depends on PlaylistItemMarker)
             */

            // Obter a lista de registros da tabela "Location" do banco de dados de origem.
            var t_Location = dbSource.TableList<Location>();

            // Para cada registro na lista da tabela "Location" do banco de origem.
            foreach (var item in t_Location)
            {
                try
                {
                    // Tentar obter registros correspondentes da tabela "Location" no banco de destino, utilizando diferentes critérios de pesquisa.
                    var location1 = dbMerged.GetFirst<Location>(item, new string[] { "KeySymbol", "IssueTagNumber", "MepsLanguage", "BookNumber", "DocumentId", "Track", "Type" });
                    var location2 = dbMerged.GetFirst<Location>(item, new string[] { "BookNumber", "ChapterNumber", "KeySymbol", "MepsLanguage", "Type" }, false);
                    var location3 = dbMerged.GetFirst<Location>(item, new string[] { "KeySymbol", "IssueTagNumber", "MepsLanguage", "DocumentId", "Track", "Type" });

                    // Verificar se nenhum dos registros correspondentes foi encontrado no banco de destino.
                    if (location1 == null && location2 == null && location3 == null)
                    {
                        // Inserir o registro da tabela "Location" no banco de destino e atualizar o campo "NewLocationId" do registro atual.
                        item.NewLocationId = dbMerged.ItemInsert<Location>(item);
                    }
                    else
                    {
                        // Caso contrário, atribuir ao campo "NewLocationId" do registro atual o valor do primeiro campo "LocationId" não nulo entre os registros correspondentes.
                        item.NewLocationId = (location1?.LocationId ?? location2?.LocationId ?? location3?.LocationId) ?? 0;
                    }
                }
                catch (Exception ex)
                {
                    // Em caso de exceção, imprimir a mensagem de erro no console.
                    Console.WriteLine(ex.Message);
                }
            }


            // Obter a lista de registros da tabela "InputField" do banco de dados de origem.
            var t_InputField_old = dbSource.TableList<InputField>();

            // Juntar (join) a lista da tabela "InputField" do banco de origem com a lista da tabela "Location" do banco de destino,
            // utilizando a correspondência entre os campos "LocationId" da tabela "InputField" e "NewLocationId" da tabela "Location".
            var t_InputField_new = t_InputField_old.Join(t_Location, i => i.LocationId, l => l.LocationId,
                (i, l) =>
                {
                    // Atualizar o campo "LocationId" do registro da tabela "InputField" para o valor "NewLocationId" correspondente da tabela "Location".
                    i.LocationId = l.NewLocationId;
                    return i;
                });

            // Para cada registro na lista da tabela "InputField" após a junção.
            foreach (var item in t_InputField_new)
            {
                // Inserir o registro da tabela "InputField" no banco de destino.
                dbMerged.ItemInsert<InputField>(item);
            }


            // Obter a lista de registros da tabela "Bookmark" do banco de dados de origem.
            var t_Bookmark_old = dbSource.TableList<Bookmark>();

            // Juntar (join) a lista da tabela "Bookmark" do banco de origem com a lista da tabela "Location" do banco de destino,
            // utilizando a correspondência entre os campos "LocationId" da tabela "Bookmark" e "NewLocationId" da tabela "Location".
            var t_Bookmark_new = t_Bookmark_old.Join(t_Location, b => b.LocationId, l => l.LocationId,
                (b, l) =>
                {
                    // Atualizar o campo "LocationId" do registro da tabela "Bookmark" para o valor "NewLocationId" correspondente da tabela "Location".
                    b.LocationId = l.NewLocationId;
                    return b;
                });

            // Para cada registro na lista da tabela "Bookmark" após a junção.
            foreach (var item in t_Bookmark_new)
            {
                // Inserir o registro da tabela "Bookmark" no banco de destino.
                dbMerged.ItemInsert<Bookmark>(item);
            }

            // Obter a lista de registros da tabela "UserMark" do banco de dados de origem.
            var t_UserMark_old = dbSource.TableList<UserMark>();

            // Juntar (join) a lista da tabela "UserMark" do banco de origem com a lista da tabela "Location" do banco de destino,
            // utilizando a correspondência entre os campos "LocationId" da tabela "UserMark" e "NewLocationId" da tabela "Location".
            var t_UserMark_new = t_UserMark_old.Join(t_Location, u => u.LocationId, l => l.LocationId,
                (b, l) =>
                {
                    // Atualizar o campo "LocationId" do registro da tabela "UserMark" para o valor "NewLocationId" correspondente da tabela "Location".
                    b.LocationId = l.NewLocationId;
                    return b;
                });

            // Para cada registro na lista da tabela "UserMark" após a junção.
            foreach (var item in t_UserMark_new)
            {
                // Inserir o registro da tabela "UserMark" no banco de destino e obter o novo ID inserido.
                item.NewUserMarkId = dbMerged.ItemInsert<UserMark>(item);
            }

            // Obter a lista de registros da tabela "BlockRange" do banco de dados de origem.
            var t_BlockRange_old = dbSource.TableList<BlockRange>();

            // Juntar (join) a lista da tabela "BlockRange" do banco de origem com a lista resultante da tabela "UserMark" do banco de destino,
            // utilizando a correspondência entre os campos "UserMarkId" da tabela "BlockRange" e "NewUserMarkId" da tabela "UserMark".
            var t_BlockRange_new = t_BlockRange_old.Join(t_UserMark_new, b => b.UserMarkId, u => u.UserMarkId,
                (b, u) =>
                {
                    // Atualizar o campo "UserMarkId" do registro da tabela "BlockRange" para o valor "NewUserMarkId" correspondente da tabela "UserMark".
                    b.UserMarkId = u.NewUserMarkId;
                    return b;
                });

            // Para cada registro na lista da tabela "BlockRange" após a junção.
            foreach (var item in t_BlockRange_new)
            {
                // Inserir o registro da tabela "BlockRange" no banco de destino.
                dbMerged.ItemInsert<BlockRange>(item);
            }
            // Obter a lista de registros da tabela "Note" do banco de dados de origem.
            var t_Note_old = dbSource.TableList<Note>();

            // Juntar (join) a lista da tabela "Note" do banco de origem com a lista resultante da tabela "Location" após a mesclagem no banco de destino,
            // utilizando a correspondência entre os campos "LocationId" da tabela "Note" e "NewLocationId" da tabela "Location".
            // Em seguida, juntar novamente a lista resultante com a lista resultante da tabela "UserMark" após a mesclagem no banco de destino,
            // utilizando a correspondência entre os campos "UserMarkId" da tabela "Note" e "NewUserMarkId" da tabela "UserMark".
            var t_Note_new = t_Note_old.Join(t_Location, n => n.LocationId, l => l.LocationId,
                (n, l) =>
                {
                    // Atualizar o campo "LocationId" do registro da tabela "Note" para o valor "NewLocationId" correspondente da tabela "Location".
                    n.LocationId = l.NewLocationId;
                    return n;
                })
                .Join(t_UserMark_new, n => n.UserMarkId, u => u.UserMarkId,
                (n, u) =>
                {
                    // Atualizar o campo "UserMarkId" do registro da tabela "Note" para o valor "NewUserMarkId" correspondente da tabela "UserMark".
                    n.UserMarkId = u.NewUserMarkId;
                    return n;
                });

            // Para cada registro na lista da tabela "Note" após as junções.
            foreach (var item in t_Note_new)
            {
                // Inserir o registro da tabela "Note" no banco de destino e obter o novo ID gerado.
                item.NewNoteId = dbMerged.ItemInsert<Note>(item);
            }
            // Obter a lista de registros da tabela "IndependentMedia" do banco de dados de origem.
            var t_IndependentMedia = dbSource.TableList<IndependentMedia>();

            // Para cada registro na lista da tabela "IndependentMedia".
            foreach (var item in t_IndependentMedia)
            {
                // Inserir o registro da tabela "IndependentMedia" no banco de destino e obter o novo ID gerado.
                item.NewIndependentMediaId = dbMerged.ItemInsert<IndependentMedia>(item);
            }

            // Obter a lista de registros da tabela "PlayListItem" do banco de dados de origem.
            var t_PlayListItem = dbSource.TableList<PlayListItem>();

            // Para cada registro na lista da tabela "PlayListItem".
            foreach (var item in t_PlayListItem)
            {
                // Inserir o registro da tabela "PlayListItem" no banco de destino e obter o novo ID gerado.
                item.NewPlaylistItemId = dbMerged.ItemInsert<PlayListItem>(item);
            }

            // Obter a lista de registros da tabela "PlaylistItemIndependentMediaMap" do banco de dados de origem.
            var t_PlayListItemIndependentMediaMap_old = dbSource.TableList<PlaylistItemIndependentMediaMap>();

            // Juntar a lista de registros da tabela "PlaylistItemIndependentMediaMap" com a lista de registros da tabela "IndependentMedia"
            // usando a coluna "IndependentMediaId" como chave de junção, e atualizar o ID do "IndependentMedia" com o novo ID gerado na tabela de destino.
            // Em seguida, juntar novamente com a lista de registros da tabela "PlayListItem", usando a coluna "PlaylistItemId" como chave de junção,
            // e atualizar o ID do "PlayListItem" com o novo ID gerado na tabela de destino.
            var t_PlayListItemIndependentMediaMap_new = t_PlayListItemIndependentMediaMap_old.Join(t_IndependentMedia, m => m.IndependentMediaId, i => i.IndependentMediaId,
                (m, i) =>
                {
                    m.IndependentMediaId = i.NewIndependentMediaId;
                    return m;
                })
                .Join(t_PlayListItem, m => m.PlaylistItemId, p => p.PlaylistItemId,
                (m, p) =>
                {
                    m.PlaylistItemId = p.NewPlaylistItemId;
                    return m;
                });

            // Para cada registro na lista da tabela "PlaylistItemIndependentMediaMap" mesclada.
            foreach (var item in t_PlayListItemIndependentMediaMap_new)
            {
                // Inserir o registro na tabela "PlaylistItemIndependentMediaMap" no banco de destino.
                dbMerged.ItemInsert<PlaylistItemIndependentMediaMap>(item);
            }

            // Obter a lista de registros da tabela "PlaylistItemLocationMap" do banco de dados de origem.
            var t_PlaylistItemLocationMap_old = dbSource.TableList<PlaylistItemLocationMap>();

            // Juntar a lista de registros da tabela "PlaylistItemLocationMap" com a lista de registros da tabela "Location"
            // usando a coluna "LocationId" como chave de junção, e atualizar o ID da "Location" com o novo ID gerado na tabela de destino.
            // Em seguida, juntar novamente com a lista de registros da tabela "PlayListItem", usando a coluna "PlaylistItemId" como chave de junção,
            // e atualizar o ID do "PlayListItem" com o novo ID gerado na tabela de destino.
            var t_PlaylistItemLocationMap_new = t_PlaylistItemLocationMap_old.Join(t_Location, m => m.LocationId, l => l.LocationId,
                (m, l) =>
                {
                    m.LocationId = l.NewLocationId;
                    return m;
                })
                .Join(t_PlayListItem, m => m.PlaylistItemId, p => p.PlaylistItemId,
                (m, p) =>
                {
                    m.PlaylistItemId = p.NewPlaylistItemId;
                    return m;
                });

            // Para cada registro na lista da tabela "PlaylistItemLocationMap" mesclada.
            foreach (var item in t_PlaylistItemLocationMap_new)
            {
                // Inserir o registro na tabela "PlaylistItemLocationMap" no banco de destino.
                dbMerged.ItemInsert<PlaylistItemLocationMap>(item);
            }

            // Obter a lista de registros da tabela "Tag" do banco de dados de origem.
            var t_Tag = dbSource.TableList<Tag>();

            // Para cada registro na lista da tabela "Tag".
            foreach (var item in t_Tag)
            {
                try
                {
                    // Tentar encontrar um registro correspondente na tabela de destino (dbMerged) usando os atributos "Type" e "Name".
                    var tag1 = dbMerged.GetFirst<Tag>(item, new string[] { "Type", "Name" });

                    if (tag1 == null)
                    {
                        // Se não foi encontrado um registro correspondente na tabela de destino,
                        // atualizar o ID da tag com o novo ID gerado e inserir o registro na tabela de destino (dbMerged).
                        item.NewTagId = dbMerged.ItemInsert<Tag>(item);
                    }
                    else
                    {
                        // Se um registro correspondente foi encontrado na tabela de destino,
                        // atualizar o ID da tag com o ID do registro correspondente na tabela de destino.
                        item.NewTagId = tag1.TagId;
                    }
                }
                catch (Exception ex)
                {
                    // Em caso de erro, imprimir a mensagem de erro no console.
                    Console.WriteLine(ex.Message);
                }
            }

            // Obter a lista de registros da tabela "TagMap" do banco de dados de origem.
            var t_TagMap_old = dbSource.TableList<TagMap>();

            // Unir a lista de TagMap com a lista de tags da tabela de destino usando o ID da tag.
            // Para cada registro correspondente, atualizar o ID da tag no registro TagMap com o novo ID gerado na tabela de destino.
            // Unir também com a lista de PlayListItem para atualizar o ID do PlayListItem.
            var t_TagMap_new = t_TagMap_old.Join(t_Tag, m => m.TagId, t => t.TagId,
                (m, t) =>
                {
                    m.TagId = t.NewTagId;
                    return m;
                })
                .Join(t_PlayListItem, m => m.PlaylistItemId, p => p.PlaylistItemId,
                (m, p) =>
                {
                    m.PlaylistItemId = p.NewPlaylistItemId;
                    return m;
                });

            // Para cada registro na lista da tabela "TagMap" após a mesclagem,
            // inserir o registro na tabela de destino (dbMerged).
            foreach (var item in t_TagMap_new)
            {
                dbMerged.ItemInsert<TagMap>(item);
            }

            // Obter a lista de registros da tabela "PlaylistItemMarker" do banco de dados de origem.
            var t_PlaylistItemMarker_old = dbSource.TableList<PlaylistItemMarker>();

            // Unir a lista de PlaylistItemMarker com a lista de PlayListItem da tabela de destino usando o ID do PlayListItem.
            // Para cada registro correspondente, atualizar o ID do PlayListItem no registro PlaylistItemMarker com o novo ID gerado na tabela de destino.
            var t_PlaylistItemMarker_new = t_PlaylistItemMarker_old.Join(t_PlayListItem, m => m.PlaylistItemId, p => p.PlaylistItemId,
                (m, p) =>
                {
                    m.PlaylistItemId = p.NewPlaylistItemId;
                    return m;
                });

            // Para cada registro na lista da tabela "PlaylistItemMarker" após a mesclagem,
            // inserir o registro na tabela de destino (dbMerged) e obter o novo ID gerado para o PlaylistItemMarker.
            foreach (var item in t_PlaylistItemMarker_new)
            {
                item.NewPlaylistItemMarkerId = dbMerged.ItemInsert<PlaylistItemMarker>(item);
            }

            // Obter a lista de registros da tabela "PlaylistItemMarkerBibleVerseMap" do banco de dados de origem.
            var t_PlaylistItemMarkerBibleVerseMap_old = dbSource.TableList<PlaylistItemMarkerBibleVerseMap>();

            // Unir a lista de PlaylistItemMarkerBibleVerseMap com a lista de PlaylistItemMarker da tabela de destino usando o ID do PlaylistItemMarker.
            // Para cada registro correspondente, atualizar o ID do PlaylistItemMarker no registro PlaylistItemMarkerBibleVerseMap com o novo ID gerado na tabela de destino.
            var t_PlaylistItemMarkerBibleVerseMap_new = t_PlaylistItemMarkerBibleVerseMap_old.Join(t_PlaylistItemMarker_new, b => b.PlaylistItemMarkerId, m => m.PlaylistItemMarkerId,
                (b, m) =>
                {
                    b.PlaylistItemMarkerId = m.NewPlaylistItemMarkerId;
                    return b;
                });

            // Para cada registro na lista da tabela "PlaylistItemMarkerBibleVerseMap" após a mesclagem,
            // inserir o registro na tabela de destino (dbMerged).
            foreach (var item in t_PlaylistItemMarkerBibleVerseMap_new)
            {
                dbMerged.ItemInsert<PlaylistItemMarkerBibleVerseMap>(item);
            }

            // Obter a lista de registros da tabela "PlaylistItemMarkerParagraphMap" do banco de dados de origem.
            var t_PlaylistItemMarkerParagraphMap_old = dbSource.TableList<PlaylistItemMarkerParagraphMap>();

            // Unir a lista de PlaylistItemMarkerParagraphMap com a lista de PlaylistItemMarker da tabela de destino usando o ID do PlaylistItemMarker.
            // Para cada registro correspondente, atualizar o ID do PlaylistItemMarker no registro PlaylistItemMarkerParagraphMap com o novo ID gerado na tabela de destino.
            var t_PlaylistItemMarkerParagraphMap_new = t_PlaylistItemMarkerParagraphMap_old.Join(t_PlaylistItemMarker_new, p => p.PlaylistItemMarkerId, m => m.PlaylistItemMarkerId,
                (p, m) =>
                {
                    p.PlaylistItemMarkerId = m.NewPlaylistItemMarkerId;
                    return p;
                });

            // Para cada registro na lista da tabela "PlaylistItemMarkerParagraphMap" após a mesclagem,
            // inserir o registro na tabela de destino (dbMerged).
            foreach (var item in t_PlaylistItemMarkerParagraphMap_new)
            {
                dbMerged.ItemInsert<PlaylistItemMarkerParagraphMap>(item);
            }

            /*
             * sys tables?
             * Tag
             * PlaylistItemAccuracy
             */
        }

        /// <summary>
        /// Gera um hash SHA-256 para um arquivo de banco de dados.
        /// </summary>
        /// <param name="dbFile">O caminho completo para o arquivo do banco de dados.</param>
        /// <returns>O hash gerado como uma sequência de caracteres em minúsculas sem hifens.</returns>
        private string GenerateDatabaseHash(string dbFile)
        {
            // Criar uma instância do algoritmo de hash SHA-256
            SHA256 sha256 = SHA256.Create();

            using var fs = new FileStream(dbFile, FileMode.Open); // Abrir o arquivo do banco de dados
            using var bs = new BufferedStream(fs); // Criar um buffer para a leitura

            // Calcular o hash SHA-256 do conteúdo do arquivo
            var hash = sha256.ComputeHash(bs);

            // Converter o hash em uma sequência de caracteres em minúsculas e sem hifens
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
            // Alternativamente, a linha acima pode ser substituída pela seguinte linha para obter o mesmo resultado:
            // return string.Join("", hash.Select(b => $"{b:x2}").ToArray());
        }


    }
}