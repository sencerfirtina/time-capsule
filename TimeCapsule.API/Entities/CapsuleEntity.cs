using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TimeCapsule.API.Entities
{
    public class CapsuleEntity
    {
        public int Id { get; set; }
        public required string EncryptedContent { get; set; }
        public TriggerType Category { get; set; }
        public TriggerOperator? Operator { get; set; }
        public required string TargetValue { get; set; }
        public string? MetaData { get; set; }
        public bool IsOpened { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;   
        public int UserID { get; set; }
        public User User { get; set; } = null!;
    }
    public enum TriggerType
    {
        Weather,
        Crypto,
        Date,
        GeoFence,
        SpotifyTrackId
    }
    public enum TriggerOperator
    {
        GreaterThan,
        LessThan,
        Equals
    }
}