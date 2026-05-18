using Microsoft.EntityFrameworkCore;
using SessionRecorder.Core.Entities;

namespace SessionRecorder.Data.Repositories;

// --- Interfaces ---

public interface ISkillDomainRepository
{
    Task<List<SkillDomain>> GetAllAsync(bool activeOnly = true);
    Task<SkillDomain?> GetByIdAsync(int id);
    Task AddAsync(SkillDomain domain);
    Task UpdateAsync(SkillDomain domain);
    Task<bool> IsInUseAsync(int id);
    Task<string> GetNextCodeAsync();
}

public interface IChildRepository
{
    Task<List<Child>> GetAllAsync(bool activeOnly = true);
    Task<Child?> GetByIdAsync(int id);
    Task<Child?> GetWithRecordsAsync(int id);
    Task AddAsync(Child child);
    Task UpdateAsync(Child child);
    Task DeleteAsync(int id);
    Task<string> GetNextCodeAsync();
}

public interface IProgramRepository
{
    Task<List<InterventionProgram>> GetAllAsync(bool activeOnly = true);
    Task<InterventionProgram?> GetByIdAsync(int id);
    Task AddAsync(InterventionProgram program);
    Task UpdateAsync(InterventionProgram program);
    Task DeleteAsync(int id);
    Task<bool> IsInUseAsync(int id);
    Task<string> GetNextCodeAsync();
}

public interface IProgramTypeRepository
{
    Task<List<ProgramTypeMaster>> GetAllAsync(bool activeOnly = true);
    Task<ProgramTypeMaster?> GetByIdAsync(int id);
    Task AddAsync(ProgramTypeMaster type);
    Task UpdateAsync(ProgramTypeMaster type);
    Task<bool> IsInUseAsync(int id);
    Task<string> GetNextCodeAsync();
}

public interface ISessionRecordRepository
{
    Task<List<SessionRecord>> GetAllAsync();
    Task<List<SessionRecord>> GetByChildIdAsync(int childId);
    Task<List<SessionRecord>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<SessionRecord?> GetByIdAsync(int id);
    Task AddAsync(SessionRecord record);
    Task UpdateAsync(SessionRecord record);
    Task DeleteAsync(int id);
    Task<List<SessionRecord>> SearchAsync(string keyword);
}

public interface INaturalObservationRepository
{
    Task<List<NaturalObservation>> GetAllAsync();
    Task<List<NaturalObservation>> GetByChildIdAsync(int childId);
    Task<NaturalObservation?> GetByIdAsync(int id);
    Task AddAsync(NaturalObservation observation);
    Task UpdateAsync(NaturalObservation observation);
    Task DeleteAsync(int id);
    Task<List<NaturalObservation>> SearchAsync(string keyword);
}

// --- Implementations ---

public class SkillDomainRepository(AppDbContext db) : ISkillDomainRepository
{
    public async Task<List<SkillDomain>> GetAllAsync(bool activeOnly = true)
    {
        var query = db.SkillDomains.AsQueryable();
        if (activeOnly) query = query.Where(d => d.IsActive);
        return await query.OrderBy(d => d.DomainCode).ToListAsync();
    }

    public async Task<SkillDomain?> GetByIdAsync(int id) =>
        await db.SkillDomains.FindAsync(id);

    public async Task AddAsync(SkillDomain domain)
    {
        db.SkillDomains.Add(domain);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(SkillDomain domain)
    {
        db.SkillDomains.Update(domain);
        await db.SaveChangesAsync();
    }

    public async Task<bool> IsInUseAsync(int id) =>
        await db.Programs.AnyAsync(p => p.DomainId == id);

    public async Task<string> GetNextCodeAsync()
    {
        var codes = await db.SkillDomains
            .Where(d => d.DomainCode != "D999")
            .Select(d => d.DomainCode)
            .ToListAsync();

        var maxNum = codes
            .Select(c => int.TryParse(c[1..], out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"D{maxNum + 1:D3}";
    }
}

public class ChildRepository(AppDbContext db) : IChildRepository
{
    public async Task<List<Child>> GetAllAsync(bool activeOnly = true)
    {
        var query = db.Children.AsQueryable();
        if (activeOnly) query = query.Where(c => c.IsActive);
        return await query.OrderBy(c => c.ChildCode).ToListAsync();
    }

    public async Task<Child?> GetByIdAsync(int id) =>
        await db.Children.FindAsync(id);

    public async Task<Child?> GetWithRecordsAsync(int id) =>
        await db.Children
            .Include(c => c.SessionRecords).ThenInclude(s => s.Program)
            .Include(c => c.NaturalObservations)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(Child child)
    {
        db.Children.Add(child);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Child child)
    {
        db.Children.Update(child);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var child = await db.Children.FindAsync(id);
        if (child == null) return;
        child.IsActive = false;
        await db.SaveChangesAsync();
    }

    public async Task<string> GetNextCodeAsync()
    {
        var maxCode = await db.Children
            .OrderByDescending(c => c.ChildCode)
            .Select(c => c.ChildCode)
            .FirstOrDefaultAsync();

        if (maxCode == null) return "K001";
        var num = int.Parse(maxCode[1..]);
        return $"K{num + 1:D3}";
    }
}

public class ProgramRepository(AppDbContext db) : IProgramRepository
{
    public async Task<List<InterventionProgram>> GetAllAsync(bool activeOnly = true)
    {
        var query = db.Programs
            .Include(p => p.Domain)
            .Include(p => p.ProgramType)
            .AsQueryable();
        if (activeOnly) query = query.Where(p => p.IsActive);
        return await query.OrderBy(p => p.ProgramCode).ToListAsync();
    }

    public async Task<InterventionProgram?> GetByIdAsync(int id) =>
        await db.Programs
            .Include(p => p.Domain)
            .Include(p => p.ProgramType)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task AddAsync(InterventionProgram program)
    {
        db.Programs.Add(program);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(InterventionProgram program)
    {
        db.Programs.Update(program);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var program = await db.Programs.FindAsync(id);
        if (program == null) return;
        program.IsActive = false;
        await db.SaveChangesAsync();
    }

    public async Task<bool> IsInUseAsync(int id) =>
        await db.SessionRecords.AnyAsync(s => s.ProgramId == id);

    public async Task<string> GetNextCodeAsync()
    {
        var maxCode = await db.Programs
            .Where(p => p.ProgramCode != "P999")
            .OrderByDescending(p => p.ProgramCode)
            .Select(p => p.ProgramCode)
            .FirstOrDefaultAsync();

        if (maxCode == null) return "P001";
        var num = int.Parse(maxCode[1..]);
        return $"P{num + 1:D3}";
    }
}

public class SessionRecordRepository(AppDbContext db) : ISessionRecordRepository
{
    public async Task<List<SessionRecord>> GetAllAsync() =>
        await db.SessionRecords
            .Include(s => s.Program).ThenInclude(p => p.Domain)
            .Include(s => s.Child)
            .OrderByDescending(s => s.Date)
            .ThenBy(s => s.Child.Name)
            .ToListAsync();

    public async Task<List<SessionRecord>> GetByChildIdAsync(int childId) =>
        await db.SessionRecords
            .Include(s => s.Program)
            .Include(s => s.Child)
            .Where(s => s.ChildId == childId)
            .OrderByDescending(s => s.Date)
            .ToListAsync();

    public async Task<List<SessionRecord>> GetByDateRangeAsync(DateTime from, DateTime to) =>
        await db.SessionRecords
            .Include(s => s.Program)
            .Include(s => s.Child)
            .Where(s => s.Date >= from && s.Date <= to)
            .OrderByDescending(s => s.Date)
            .ToListAsync();

    public async Task<SessionRecord?> GetByIdAsync(int id) =>
        await db.SessionRecords
            .Include(s => s.Program)
            .Include(s => s.Child)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task AddAsync(SessionRecord record)
    {
        db.SessionRecords.Add(record);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(SessionRecord record)
    {
        db.SessionRecords.Update(record);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var record = await db.SessionRecords.FindAsync(id);
        if (record != null)
        {
            db.SessionRecords.Remove(record);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<SessionRecord>> SearchAsync(string keyword) =>
        await db.SessionRecords
            .Include(s => s.Program)
            .Include(s => s.Child)
            .Where(s =>
                (s.ClinicalNote != null && s.ClinicalNote.Contains(keyword)) ||
                (s.Hypothesis != null && s.Hypothesis.Contains(keyword)) ||
                (s.NextAction != null && s.NextAction.Contains(keyword)))
            .OrderByDescending(s => s.Date)
            .ToListAsync();
}

public class NaturalObservationRepository(AppDbContext db) : INaturalObservationRepository
{
    public async Task<List<NaturalObservation>> GetAllAsync() =>
        await db.NaturalObservations
            .Include(o => o.Child)
            .OrderByDescending(o => o.Date)
            .ThenBy(o => o.Child.Name)
            .ToListAsync();

    public async Task<List<NaturalObservation>> GetByChildIdAsync(int childId) =>
        await db.NaturalObservations
            .Include(o => o.Child)
            .Where(o => o.ChildId == childId)
            .OrderByDescending(o => o.Date)
            .ToListAsync();

    public async Task<NaturalObservation?> GetByIdAsync(int id) =>
        await db.NaturalObservations
            .Include(o => o.Child)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task AddAsync(NaturalObservation observation)
    {
        db.NaturalObservations.Add(observation);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(NaturalObservation observation)
    {
        db.NaturalObservations.Update(observation);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var obs = await db.NaturalObservations.FindAsync(id);
        if (obs != null)
        {
            db.NaturalObservations.Remove(obs);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<NaturalObservation>> SearchAsync(string keyword) =>
        await db.NaturalObservations
            .Include(o => o.Child)
            .Where(o =>
                (o.Situation != null && o.Situation.Contains(keyword)) ||
                (o.ObservedBehavior != null && o.ObservedBehavior.Contains(keyword)) ||
                (o.Interpretation != null && o.Interpretation.Contains(keyword)) ||
                (o.NextVerification != null && o.NextVerification.Contains(keyword)))
            .OrderByDescending(o => o.Date)
            .ToListAsync();
}

public class ProgramTypeRepository(AppDbContext db) : IProgramTypeRepository
{
    public async Task<List<ProgramTypeMaster>> GetAllAsync(bool activeOnly = true)
    {
        var query = db.ProgramTypes.AsQueryable();
        if (activeOnly) query = query.Where(t => t.IsActive);
        return await query.OrderBy(t => t.Id).ToListAsync();
    }

    public async Task<ProgramTypeMaster?> GetByIdAsync(int id) =>
        await db.ProgramTypes.FindAsync(id);

    public async Task AddAsync(ProgramTypeMaster type)
    {
        db.ProgramTypes.Add(type);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProgramTypeMaster type)
    {
        db.ProgramTypes.Update(type);
        await db.SaveChangesAsync();
    }

    public async Task<bool> IsInUseAsync(int id) =>
        await db.Programs.AnyAsync(p => p.ProgramTypeId == id && p.IsActive);

    public async Task<string> GetNextCodeAsync()
    {
        // ユーザー追加分は "T001" 形式の連番（既存の予約コードと衝突しない範囲）
        var existing = await db.ProgramTypes
            .Where(t => t.TypeCode.StartsWith("T"))
            .Select(t => t.TypeCode)
            .ToListAsync();

        var maxNum = existing
            .Select(c => int.TryParse(c[1..], out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"T{maxNum + 1:D3}";
    }
}
