(() => {
  const conserto = document.getElementById("fezConserto");
  const selagem = document.getElementById("fezSelagem");
  const consertoFields = document.querySelector("[data-conserto-fields]");
  const selagemFields = document.querySelector("[data-selagem-fields]");
  const categoria = document.querySelector('[name="Input.CategoriaTarifa"]');
  const gru = document.querySelectorAll('input[name="Input.IncluiGru"]');
  const preview = document.querySelector("[data-tariff-preview]");

  if (!conserto || !selagem || !consertoFields || !selagemFields || !categoria || !preview) {
    return;
  }

  const moeda = new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  });

  function alternarCampos(controle, campos) {
    const ativo = controle.checked;
    campos.hidden = !ativo;
    controle.setAttribute("aria-expanded", String(ativo));

    for (const campo of campos.querySelectorAll("input, select, textarea")) {
      campo.disabled = !ativo;
    }
  }

  function atualizarTarifa() {
    if (!selagem.checked) {
      preview.textContent = "Selecione as opções para ver o valor.";
      return;
    }

    const incluiGru = document.querySelector('input[name="Input.IncluiGru"]:checked');
    if (!categoria.value || !incluiGru) {
      preview.textContent = "Selecione as opções para ver o valor.";
      return;
    }

    const escolar = categoria.value === "1";
    const comGru = incluiGru.value === "true";
    const chave = escolar
      ? comGru
        ? "escolarComGru"
        : "escolarSemGru"
      : comGru
        ? "pesadoComGru"
        : "pesadoSemGru";
    preview.textContent = `Valor calculado: ${moeda.format(Number(selagemFields.dataset[chave]))}`;
  }

  function atualizarVisibilidade() {
    alternarCampos(conserto, consertoFields);
    alternarCampos(selagem, selagemFields);
    atualizarTarifa();
  }

  conserto.addEventListener("change", atualizarVisibilidade);
  selagem.addEventListener("change", atualizarVisibilidade);
  categoria.addEventListener("change", atualizarTarifa);
  for (const item of gru) {
    item.addEventListener("change", atualizarTarifa);
  }

  atualizarVisibilidade();
})();
