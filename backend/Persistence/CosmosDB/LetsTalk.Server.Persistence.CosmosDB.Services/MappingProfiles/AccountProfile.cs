using AutoMapper;
using LetsTalk.Server.Persistence.AgnosticServices.Models;
using LetsTalk.Server.Persistence.CosmosDB.Models;

namespace LetsTalk.Server.Persistence.CosmosDB.Services.MappingProfiles;

public class AccountProfile: Profile
{
    public AccountProfile()
    {
        CreateMap<Account, AccountServiceModel>();
        CreateMap<Account, ProfileServiceModel>();
    }
}
