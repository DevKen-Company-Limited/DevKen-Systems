using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Library
{
    public class BookRecommendationRepository : RepositoryBase<BookRecommendation, Guid>, IBookRecommendationRepository
    {
        public BookRecommendationRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<IEnumerable<BookRecommendation>> GetAllByTenantAsync(Guid tenantId, bool trackChanges = false)
        {
            return await FindByCondition(
        // If tenantId is empty, return everything (for SuperAdmin)
                br => tenantId == Guid.Empty || br.TenantId == tenantId,
                trackChanges)
                .Include(br => br.Book)    // <--- Add this
                .Include(br => br.Student) // <--- Add this
                .OrderByDescending(br => br.Score)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookRecommendation>> GetByStudentIdAsync(Guid tenantId, Guid studentId, bool trackChanges = false)
        {
            return await FindByCondition(
                br => br.TenantId == tenantId && br.StudentId == studentId,
                trackChanges)
                .Include(br => br.Book)    // <--- Add this
                .OrderByDescending(br => br.Score)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookRecommendation>> GetByBookIdAsync(Guid tenantId, Guid bookId, bool trackChanges = false)
        {
            return await FindByCondition(
                br => br.TenantId == tenantId && br.BookId == bookId,
                trackChanges)
                .OrderByDescending(br => br.Score)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookRecommendation>> GetTopRecommendationsAsync(Guid tenantId, Guid studentId, int topN = 10, bool trackChanges = false)
        {
            return await FindByCondition(
                br => br.TenantId == tenantId && br.StudentId == studentId,
                trackChanges)
                .OrderByDescending(br => br.Score)
                .Take(topN)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(Guid tenantId, Guid bookId, Guid studentId)
        {
            return await FindByCondition(
                br => br.TenantId == tenantId && br.BookId == bookId && br.StudentId == studentId,
                trackChanges: false)
                .AnyAsync();
        }

        public async Task<BookRecommendation?> GetByBookAndStudentAsync(Guid tenantId, Guid bookId, Guid studentId, bool trackChanges = false)
        {
            return await FindByCondition(
                br => br.TenantId == tenantId && br.BookId == bookId && br.StudentId == studentId,
                trackChanges)
                .FirstOrDefaultAsync();
        }

        public async Task DeleteByStudentIdAsync(Guid tenantId, Guid studentId)
        {
            var recommendations = await FindByCondition(
                br => br.TenantId == tenantId && br.StudentId == studentId,
                trackChanges: true)
                .ToListAsync();

            foreach (var recommendation in recommendations)
            {
                Delete(recommendation);
            }
        }

        public async Task DeleteByBookIdAsync(Guid tenantId, Guid bookId)
        {
            var recommendations = await FindByCondition(
                br => br.TenantId == tenantId && br.BookId == bookId,
                trackChanges: true)
                .ToListAsync();

            foreach (var recommendation in recommendations)
            {
                Delete(recommendation);
            }
        }
    }
}