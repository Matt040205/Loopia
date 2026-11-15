"""Medicao do confronto com o Morcego Anciao por cenario de cartas (estabilizacao do MVP, P1-3).

Uso (Editor da worktree em Play Mode: tools/visual-baseline/run_baseline.py play):
  python -I tools/balanceamento/medir_chefe.py <saida.json> [cenario ...]

Cada partida: recarrega a HexPrototipo, fixa a semente do MAPA, coloca as cartas do cenario pelo mesmo
MapaDePlataformas.Colocar da UI, liga os morcegos comuns e roda a jornada em timeScale 4 ate vitoria/derrota.
Limites: spawn de inimigos, Revoada e compra de cartas usam UnityEngine.Random sem semente; cada linha e UMA
amostra, nao uma estatistica. As cartas compradas ao subir de nivel ficam na mao (ninguem as joga).
O Play Mode grava no save do Editor: faca backup antes e restaure depois.
"""
import json, os, sys, time

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "tools", "visual-baseline"))
import ub  # noqa: E402

CS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "cs")
INICIAIS = ["Mola", "Coracao", "Moedas", "Flechas"]
CENARIOS = {
    "1_sem_dano": INICIAIS,
    "2_torre": INICIAIS + ["Torre"],
    "3_flechas_torre": INICIAIS + ["Flechas", "Torre"],
    "4_sobrevivencia_ofensivas": INICIAIS + ["Torre", "AreaDeTreino", "FonteRestauradora", "Santuario", "OficinaDeArqueiro"],
    "5_mao_parada": [],  # ninguem joga carta: a mao fica cheia e os niveis viram pendentes
    # Depois da troca Moedas -> Torre na mao garantida (P1-3): so as 4 cartas que o jogador recebe ao comecar.
    "6_mao_inicial_nova": ["Mola", "Coracao", "Torre", "Flechas"],
}
SEMENTES = [1, 2, 27]
ESCALA = 4
LIMITE_REAL = 240


def codigo(nome, **repl):
    with open(os.path.join(CS, nome), encoding="utf-8") as f:
        texto = f.read()
    for k, v in repl.items():
        texto = texto.replace("%%" + k + "%%", str(v))
    return texto


def jl(s):
    return json.loads(s) if isinstance(s, str) else s


def partida(cenario, semente):
    ub.run_code("UnityEngine.Time.timeScale = 0f; UnityEngine.SceneManagement.SceneManager.LoadScene(\"HexPrototipo\"); return true;")
    time.sleep(4)
    prep = jl(ub.run_code(codigo("preparar.cs", SEMENTE=semente, CARTAS=",".join(CENARIOS[cenario]), MORCEGOS="true")))
    time.sleep(1)
    ub.run_code("UnityEngine.Time.timeScale = %df; return true;" % ESCALA)
    inicio = time.time()
    niveis_por_volta, ultimo, chegada = {}, None, None
    while time.time() - inicio < LIMITE_REAL:
        time.sleep(2)
        e = jl(ub.run_code(codigo("estado.cs")))
        niveis_por_volta.setdefault(e["voltas"], e["nivel"])
        if chegada is None and e["estado"] == "Chefe":
            chegada = e
        ultimo = e
        if e["terminou"]:
            break
    ub.run_code("UnityEngine.Time.timeScale = 0f; return true;")
    fim = jl(ub.run_code(codigo("final.cs")))
    return {"cenario": cenario, "semente": semente, "preparo": prep, "chegadaAoChefe": chegada, "ultimoEstado": ultimo,
            "nivelNoInicioDaVolta": niveis_por_volta, "metricas": fim, "segundosReais": round(time.time() - inicio, 1)}


def main():
    saida = sys.argv[1]
    cenarios = sys.argv[2:] or list(CENARIOS)
    resultados = []
    if os.path.exists(saida):
        resultados = json.load(open(saida, encoding="utf-8"))
    feitos = {(r["cenario"], r["semente"]) for r in resultados}
    for c in cenarios:
        for s in SEMENTES:
            if (c, s) in feitos:
                continue
            r = partida(c, s)
            m = r["metricas"]
            print("%-28s s%-3d %-8s chefe %3s PV | flechas %2s (+%s outros) | sem flechas %4.1fs | dano %s/%s | %5.1fs reais" % (
                c, s, m["resultado"], m["chefePvNoFim"], m["flechasNoChefe"], m["flechasEmOutrosNoChefe"],
                m["tempoSemFlechasNoChefe"], m["danoRecebidoNoChefe"], m["pvNoInicioDoChefe"], r["segundosReais"]), flush=True)
            resultados.append(r)
            with open(saida, "w", encoding="utf-8") as f:
                json.dump(resultados, f, indent=1, ensure_ascii=False)


if __name__ == "__main__":
    main()
