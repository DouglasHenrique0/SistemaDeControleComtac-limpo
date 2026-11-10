using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Tests;

public static class RegistroBuilder
{
    public static RegistroVeiculo Criar(DateTime entrada, string placa = "ABC1D23") => new()
    {
        Placa = placa,
        Empresa = "Empresa Teste",
        NumeroDoDia = 1,
        OSComtac = "OS-001",
        Frota = "F-10",
        Cliente = "Cliente Teste",
        ModeloTCO = "Modelo X",
        NumeroSerie = "12345678",
        DataHoraEntrada = entrada,
        Status = RegistroStatus.NoPatio
    };
}
