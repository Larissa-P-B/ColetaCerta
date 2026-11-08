using ColetaCerta.Api.Data;
using ColetaCerta.Api.DTOs.PontosColeta;
using ColetaCerta.Api.Entities;
using ColetaCerta.Api.Exceptions;
using MySql.Data.MySqlClient;

using Microsoft.EntityFrameworkCore;

namespace ColetaCerta.Api.Services;

public class PontoColetaService : IPontoColetaService
{
    private readonly AppDbContext _context;

    public PontoColetaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PontoColetaResponse> CriarAsync(
        PontoColetaRequest request,
        CancellationToken cancellationToken)
    {
        var ponto = new PontoColeta(
            request.Nome,
            request.Endereco,
            request.Bairro,
            request.Cidade);

        _context.PontosColeta.Add(ponto);

        await _context.SaveChangesAsync(cancellationToken);

        return new PontoColetaResponse(
            ponto.Id,
            ponto.Nome,
            ponto.Endereco,
            ponto.Bairro,
            ponto.Cidade);
    }

    public async Task<List<PontoColetaResponse>> ListarAsync(
        CancellationToken cancellationToken)
    {
        return await _context.PontosColeta
            .AsNoTracking()
            .OrderBy(p => p.Nome)
            .ThenBy(p => p.Id)
            .Select(p => new PontoColetaResponse(
                p.Id,
                p.Nome,
                p.Endereco,
                p.Bairro,
                p.Cidade))
            .ToListAsync(cancellationToken);
    }

    public async Task<PontoColetaResponse?> ObterPorIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await _context.PontosColeta
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PontoColetaResponse(
                p.Id,
                p.Nome,
                p.Endereco,
                p.Bairro,
                p.Cidade))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> AtualizarAsync(
    int id,
    PontoColetaRequest request,
    CancellationToken cancellationToken)
    {
        var ponto = await _context.PontosColeta
            .SingleOrDefaultAsync(
                p => p.Id == id,
                cancellationToken);

        if (ponto is null)
        {
            return false;
        }

        ponto.AtualizarDados(
            request.Nome,
            request.Endereco,
            request.Bairro,
            request.Cidade);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ExcluirAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var ponto = await _context.PontosColeta
            .SingleOrDefaultAsync(
                p => p.Id == id,
                cancellationToken);

        if (ponto is null)
        {
            return false;
        }

        var possuiLixeiras = await _context.Lixeiras
            .AnyAsync(
                l => l.PontoColetaId == id,
                cancellationToken);

        if (possuiLixeiras)
        {
            throw new ConflitoException(
                "O ponto de coleta possui lixeiras vinculadas " +
                "e não pode ser excluído.");
        }

        _context.PontosColeta.Remove(ponto);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is MySqlException { Number: 1451 })
        {
            // Protege também se uma lixeira for vinculada
            // entre a consulta anterior e a exclusão.
            throw new ConflitoException(
                "O ponto de coleta possui registros vinculados " +
                "e não pode ser excluído.");
        }

        return true;
    }
}