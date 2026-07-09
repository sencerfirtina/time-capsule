using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeCapsule.API.Entities;

namespace TimeCapsule.API.DTO
{
    public class CreateCapsuleDTO
    {
        public required string EncryptedContent { get; set; }
        public TriggerType Category { get; set; }
        public TriggerOperator Operator { get; set; }
        public required string TargetValue { get; set; }
        public string? MetaData { get; set; }
    }
}