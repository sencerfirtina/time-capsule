using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeCapsule.API.Entities;

namespace TimeCapsule.API.Data.Repositories
{
    public interface ICapsuleRepository
    {
        Task AddCapsuleAsync(CapsuleEntity capsule);
        Task<bool> SaveAsync();

    }
}