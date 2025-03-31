using AutoMapper;
using AutoMapper.QueryableExtensions;
using Newtonsoft.Json;
using SocialNetwork.Data;
using SocialNetwork.DataProcessor.ExportDTOs;
using System.Globalization;
using System.Text;
using System.Xml.Serialization;

namespace SocialNetwork.DataProcessor
{
    public class Serializer
    {
        public static string ExportUsersWithFriendShipsCountAndTheirPosts(SocialNetworkDbContext dbContext)
        {
            IMapper mapper = CreateMapper();

           
            var users = dbContext
                .Users
                .Select(u => new ExportUserDto
                {
                    Username = u.Username,
                    Friendships = dbContext.Friendships.Count(f => f.UserOneId == u.Id || f.UserTwoId == u.Id),
                    Posts = u.Posts
                        .OrderBy(p => p.Id)
                        .Select(p => new ExportPostDto
                        {
                            Content = p.Content,
                            CreatedAt = p.CreatedAt.ToString("yyyy-MM-ddТHH:mm:ss", CultureInfo.InvariantCulture)
                        })
                        .ToArray()
                })
                .OrderBy(u => u.Username)
                .ToArray();

           
            var usersRootDto = new ExportUsersRootDto
            {
                Users = users
            };

            
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            var serializer = new XmlSerializer(typeof(ExportUsersRootDto));
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, usersRootDto, namespaces);
            }

            return sb.ToString().TrimEnd();
        }

        public static string ExportConversationsWithMessagesChronologically(SocialNetworkDbContext dbContext)
        {
            
            var conversations = dbContext
                .Conversations
                .OrderBy(c => c.StartedAt)
                .Select(c => new ExportConversationDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    StartedAt = c.StartedAt.ToString("yyyy-MM-ddТHH:mm:ss", CultureInfo.InvariantCulture),
                    Messages = c.Messages
                        .OrderBy(m => m.SentAt)
                        .Select(m => new ExportMessageDto
                        {
                            Content = m.Content,
                            SentAt = m.SentAt.ToString("yyyy-MM-ddТHH:mm:ss", CultureInfo.InvariantCulture),
                            Status = (int)m.Status,
                            SenderUsername = m.Sender.Username
                        })
                        .ToArray()
                })
                .ToArray();

            
            var json = JsonConvert.SerializeObject(conversations, Formatting.Indented);
            return json;
        }

        private static IMapper CreateMapper()
        {
            return new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<SocialNetworkProfile>();
            }));
        }
    }
}
