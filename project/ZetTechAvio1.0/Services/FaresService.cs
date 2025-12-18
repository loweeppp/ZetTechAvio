using Microsoft.EntityFrameworkCore;
using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;

namespace ZetTechAvio1._0.Services
{
    public interface IFaresService
    {
        Task<List<Fare>> GetAllFaresAsync();
        Task<Fare?> GetFareByIdAsync(int id);
    }

    public class FaresService : IFaresService
    {
        private readonly ApplicationDbContext _dbContext;

        public FaresService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Fare>> GetAllFaresAsync()
        {
            return await _dbContext.Fares.ToListAsync();
        }

        public async Task<Fare?> GetFareByIdAsync(int id)
        {
            return await _dbContext.Fares.FindAsync(id);
        }

    }
}