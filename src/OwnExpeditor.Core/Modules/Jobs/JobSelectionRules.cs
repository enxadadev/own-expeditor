namespace OwnExpeditor.Core.Modules.Jobs;

/// <summary>Regras puras da seleção de frete (País → Cidade → Empresa): revalidação em cascata, troca, próximo frete.</summary>
public static class JobSelectionRules
{
    /// <summary>
    /// Revalida a cadeia País① → Cidade① → Empresa① → Vaga → País② → Cidade② → Empresa② → Carga: o que deixou de valer é limpo
    /// e opção única é escolhida sozinha. Sem empresa de saída a chegada não é tocada; <paramref name="departureLocked"/>
    /// (edição de pendente) não mexe nos campos de saída.
    /// </summary>
    public static JobSelectionResult Revalidate(JobBoard board, JobSelection selection, bool departureLocked = false)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(selection);
        var state = new Chain();
        var s = selection;

        if (!departureLocked)
        {
            var fromCountry = board.HasCountries ? s.FromCountry : null;
            var countries = board.HasCountries ? board.DepartureCountries().Select(c => c.Country).ToList() : [];
            fromCountry = state.Pick(JobField.FromCountry, s.FromCountry, fromCountry, countries, true);

            var cityOptions = board.DepartureCities(fromCountry).Select(c => c.City).ToList();
            var fromCity = state.Pick(JobField.FromCity, s.FromCity, s.FromCity, cityOptions, true);

            var companies = fromCity is null ? [] : board.DepartureCompanies(fromCity);
            var fromCompany = state.Pick(JobField.FromCompany, s.FromCompany, s.FromCompany, companies, true);

            var slot = ValidateSlot(board, s.Slot, fromCompany, out var auto);
            state.Record(JobField.Slot, s.Slot, slot, auto);
            s = s with { FromCountry = fromCountry, FromCity = fromCity, FromCompany = fromCompany, Slot = slot };
        }

        if (s.FromCompany is { } from)
        {
            var toCountry = board.HasCountries ? s.ToCountry : null;
            var countries = board.HasCountries ? board.ArrivalCountries(from).Select(c => c.Country).ToList() : [];
            toCountry = state.Pick(JobField.ToCountry, s.ToCountry, toCountry, countries, true);

            var toCity = state.Pick(JobField.ToCity, s.ToCity, s.ToCity, board.ArrivalCities(from, toCountry), true);
            var toCompanies = toCity is null ? [] : board.ArrivalCompanies(from, toCity);
            var toCompany = state.Pick(JobField.ToCompany, s.ToCompany, s.ToCompany, toCompanies, true);
            var cargos = toCompany is null ? [] : board.Cargos(from, toCompany);
            var cargo = state.Pick(JobField.Cargo, s.Cargo, s.Cargo, cargos, true);
            s = s with { ToCountry = toCountry, ToCity = toCity, ToCompany = toCompany, Cargo = cargo };
        }

        return new JobSelectionResult(s, state.Cleared, state.AutoPicked);
    }

    /// <summary>Troca saída e chegada (país e cidade); limpa empresas, vaga e carga. Não escolhe nada sozinha.</summary>
    public static JobSelectionResult Swap(JobBoard board, JobSelection selection)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(selection);
        var cleared = new List<JobField>();
        var fromCountry = selection.ToCountry;
        var fromCity = selection.ToCity;
        var departureOk = fromCity is not null && board.DepartureCities(null).Any(c => c.City == fromCity);
        if (!departureOk)
        {
            if (fromCountry is not null) cleared.Add(JobField.FromCountry);
            if (fromCity is not null) cleared.Add(JobField.FromCity);
            fromCountry = null;
            fromCity = null;
        }
        if (selection.FromCompany is not null) cleared.Add(JobField.FromCompany);
        if (selection.Slot is not null) cleared.Add(JobField.Slot);
        if (selection.ToCompany is not null) cleared.Add(JobField.ToCompany);
        if (selection.Cargo is not null) cleared.Add(JobField.Cargo);
        var result = new JobSelection(fromCountry, fromCity, ToCountry: selection.FromCountry, ToCity: selection.FromCity);
        return new JobSelectionResult(result, cleared, []);
    }

    /// <summary>Depois de adicionar: limpa vaga, empresa de chegada e carga; a saída e o país/cidade de chegada ficam.</summary>
    public static JobSelectionResult NextJob(JobBoard board, JobSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        return Revalidate(board, selection with { Slot = null, ToCompany = null, Cargo = null });
    }

    /// <summary>Campos que faltam, na ordem da cadeia (países só com países; vaga só com 2+ opções).</summary>
    public static IReadOnlyList<JobField> Missing(JobBoard board, JobSelection selection)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(selection);
        List<JobField> missing = [];
        if (board.HasCountries && selection.FromCountry is null) missing.Add(JobField.FromCountry);
        if (selection.FromCity is null) missing.Add(JobField.FromCity);
        if (selection.FromCompany is null) missing.Add(JobField.FromCompany);
        if (selection.FromCompany is { } from && selection.Slot is null && SlotOptionCount(board, from) >= 2) missing.Add(JobField.Slot);
        if (board.HasCountries && selection.ToCountry is null) missing.Add(JobField.ToCountry);
        if (selection.ToCity is null) missing.Add(JobField.ToCity);
        if (selection.ToCompany is null) missing.Add(JobField.ToCompany);
        if (selection.Cargo is null) missing.Add(JobField.Cargo);
        return missing;
    }

    /// <summary>Opções de vaga: "vaga livre" (se houver) mais cada oferta substituível.</summary>
    public static int SlotOptionCount(JobBoard board, CompanyLocation from)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(from);
        return (board.FreeSlots(from) > 0 ? 1 : 0) + board.ReplaceableOffers(from).Count;
    }

    /// <summary>Pedido de frete da seleção; nulo se faltar empresa, vaga ou carga. <paramref name="fillSlotId"/> só vale em vaga livre.</summary>
    public static JobRequest? ToRequest(JobSelection selection, int expirationHours, string? fillSlotId = null)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (selection.FromCompany is not { } from || selection.ToCompany is not { } to || selection.Cargo is not { } cargo
            || selection.Slot is not { } slot) return null;
        return slot.Mode == JobSlotMode.ReplaceOffer
            ? new JobRequest(from, to, cargo, JobSlotMode.ReplaceOffer, slot.OfferId, ExpirationHours: expirationHours)
            : new JobRequest(from, to, cargo, JobSlotMode.FillEmpty, ExpirationHours: expirationHours, FillSlotId: fillSlotId);
    }

    private static JobSlotChoice? ValidateSlot(JobBoard board, JobSlotChoice? current, CompanyLocation? company,
        out bool autoPicked)
    {
        autoPicked = false;
        if (company is null) return null;
        var options = SlotOptionCount(board, company);
        var valid = current switch
        {
            null => false,
            { Mode: JobSlotMode.FillEmpty } => board.FreeSlots(company) > 0,
            _ => board.ReplaceableOffers(company).Any(o => o.OfferId == current.OfferId),
        };
        if (valid) return current;
        JobSlotChoice? pick = board.FreeSlots(company) > 0 ? JobSlotChoice.Free
            : options == 1 ? JobSlotChoice.Replace(board.ReplaceableOffers(company)[0].OfferId)
            : null;
        autoPicked = pick is not null;
        return pick;
    }

    private sealed class Chain
    {
        public List<JobField> Cleared { get; } = [];
        public List<JobField> AutoPicked { get; } = [];

        public void Record<T>(JobField field, T? before, T? after, bool auto) where T : class
        {
            if (before is not null && !Equals(before, after)) Cleared.Add(field);
            if (auto) AutoPicked.Add(field);
        }

        /// <summary>Mantém <paramref name="value"/> se estiver nas opções; senão limpa. Sem valor e com opção única, escolhe-a.</summary>
        public T? Pick<T>(JobField field, T? original, T? value, IReadOnlyList<T> options, bool autoPick) where T : class
        {
            if (value is not null && options.Contains(value)) return value;
            var picked = options.Count == 1 && autoPick ? options[0] : null;
            if (original is not null) Cleared.Add(field);
            if (picked is not null) AutoPicked.Add(field);
            return picked;
        }
    }
}
