namespace SistemaDeControleComtac.Services;

public static class DataOperacionalService
{
    public static DateTime Obter(DateTime dataHoraEntrada, DateTime? dataHoraSaida) =>
        dataHoraSaida ?? dataHoraEntrada;
}
