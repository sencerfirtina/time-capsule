using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TimeCapsule.API.Entities
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public UserSpotifyToken? SpotifyToken { get; set; }
        public ICollection<CapsuleEntity> TimeCapsules { get; set; } = new List<CapsuleEntity>();
    }
}