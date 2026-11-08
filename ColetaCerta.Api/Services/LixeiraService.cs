using ColetaCerta.Api.Data;
using ColetaCerta.Api.DTOs.Lixeiras;
using ColetaCerta.Api.Entities;
using ColetaCerta.Api.Exceptions;
using ColetaCerta.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace ColetaCerta.Api.Services;

public class LixeiraService : ILixeiraService
{
    private readonly AppDbContext _context;

    public LixeiraService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LixeiraResponse> CriarAsync(
        LixeiraRequest request,
        CancellationToken cancellationToken)
    {
        var pontoExiste = await _context.PontosColeta
            .AnyAsync(
                p => p.Id == request.PontoColetaId,
                cancellationToken);

        if (!pontoExiste)
        {
            throw new ArgumentException(
                "O ponto de coleta informado não existe.");
        }

        var codigo = request.Codigo.Trim();

        var codigoExiste = await _context.Lixeiras
            .AnyAsync(
                l => l.Codigo == codigo,
                cancellationToken);

        if (codigoExiste)
        {
            throw new ConflitoException(
                "Já existe uma lixeira com esse código.");
        }

        var lixeira = new Lixeira(
            codigo,
            request.CapacidadeLitros,
            request.PontoColetaId);

        _context.Lixeiras.Add(lixeira);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is MySqlException { Number: 1062 })
        {
            throw new ConflitoException(
                "Já existe uma lixeira com esse código.");
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is MySqlException { Number: 1452 })
        {
            throw new ConflitoException(
                "O ponto de coleta deixou de estar disponível. " +
                "Consulte os pontos e tente novamente.");
        }

        return new LixeiraResponse(
            lixeira.Id,
            lixeira.Codigo,
            lixeira.CapacidadeLitros,
            lixeira.PontoColetaId);
    }

    public async Task<List<LixeiraResponse>> ListarAsync(
        CancellationToken cancellationToken)
    {
        return await _context.Lixeiras
            .AsNoTracking()
            .OrderBy(l => l.Codigo)
            .ThenBy(l => l.Id)
            .Select(l => new LixeiraResponse(
                l.Id,
                l.Codigo,
                l.CapacidadeLitros,
                l.PontoColetaId))
            .ToListAsync(cancellationToken);
    }

    public async Task<LixeiraResponse?> ObterPorIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await _context.Lixeiras
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new LixeiraResponse(
                l.Id,
                l.Codigo,
                l.CapacidadeLitros,
                l.PontoColetaId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> AtualizarAsync(
    int id,
    LixeiraRequest request,
    CancellationToken cancellationToken)
    {
        var lixeira = await _context.Lixeiras
            .SingleOrDefaultAsync(
                l => l.Id == id,
                cancellationToken);

        if (lixeira is null)
        {
            return false;
        }

        var pontoExiste = await _context.PontosColeta
            .AnyAsync(
                p => p.Id == request.PontoColetaId,
                cancellationToken);

        if (!pontoExiste)
        {
            throw new ArgumentException(
                "O ponto de coleta informado não existe.");
        }

        var codigo = request.Codigo.Trim();

        var codigoExiste = await _context.Lixeiras
            .AnyAsync(
                l => l.Codigo == codigo && l.Id != id,
                cancellationToken);

        if (codigoExiste)
        {
            throw new ConflitoException(
                "Já existe outra lixeira com esse código.");
        }

        lixeira.AtualizarDados(
            codigo,
            request.CapacidadeLitros,
            request.PontoColetaId);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is MySqlException { Number: 1062 })
        {
            throw new ConflitoException(
                "Já existe outra lixeira com esse código.");
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is MySqlException { Number: 1452 })
        {
            throw new ConflitoException(
                "O ponto de coleta deixou de estar disponível. " +
                "Consulte os pontos e tente novamente.");
        }

        return true;
    }

    public async Task<bool> ExcluirAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var lixeira = await _context.Lixeiras
            .SingleOrDefaultAsync(
                l => l.Id == id,
                cancellationToken);

        if (lixeira is null)
        {
            return false;
        }

        _context.Lixeiras.Remove(lixeira);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is MySqlException { Number: 1451 })
        {
            throw new ConflitoException(
                "A lixeira possui registros vinculados " +
                "e não pode ser excluída.");
        }

        return true;
    }
}