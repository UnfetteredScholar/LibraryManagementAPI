using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace LibraryManagementAPI.Models.User
{
    public class UserOut
    {

        public UserOut(User user)
        {
            Id = user.Id;
            Roles = user.Roles;
            Email = user.Email;
            Name = user.Name;
        }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public List<string> Roles { get; set; } = new List<string>();
    }
}
