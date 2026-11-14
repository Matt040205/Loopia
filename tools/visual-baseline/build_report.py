"""Consolida docs/art/audit/_raw_*.json em medidas.json e MEDIDAS.md (nao fala com o Unity)."""
import json, os, re, statistics as st

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
A = os.path.join(ROOT, "docs", "art", "audit")
SEEDS = [12345, 7, 2024]


def load(n):
    with open(os.path.join(A, n), encoding="utf-8") as f:
        return json.load(f)


def stats(v):
    v = sorted(v)
    return {"min": v[0], "mediana": st.median(v), "max": v[-1], "n": len(v)}


def r(x, n=3):
    return round(x, n) if isinstance(x, (int, float)) else x


def bg_group(name):
    return re.sub(r"\s*\(\d+\)$", "", name)


def main():
    raws = {s: load("_raw_seed%d.json" % s) for s in SEEDS}
    light = load("_raw_lighting.json")
    stt = load("_raw_stats.json")
    prefabs = load("_prefabs_raw.json")
    hier = load("_hierarchy_roots.json")
    out = {"aviso": "Valores lidos do Unity Editor 6000.3.10f1 em Play Mode (nao build). 1 unidade de mundo = 1 m.",
           "hierarquia_raizes": hier, "prefabs_brutos": prefabs, "iluminacao": light, "estatisticas_render_editor": stt, "por_seed": {}}
    md = []
    for s in SEEDS:
        c = raws[s]["cena"]
        ilhas = c["ilhas"]
        ent = {}
        ent["n_ilhas"] = len(ilhas)
        ent["orthoSize_1080p"] = c["camera1080p"]["orthoSize"]
        ent["px_por_unidade_mundo"] = c["pixelsPorUnidadeDeMundo_ortho"]
        ent["ilha_modelo_bounds_size"] = ilhas[0]["modeloBoundsMundo"]["size"]
        ent["ilha_topo_y_relativo_ao_tile"] = stats([i["modeloTopoRelativoAoTile"] for i in ilhas])
        ent["ilha_collider_topo_y_relativo"] = stats([i["colliderTopRelativoAoTile"] for i in ilhas])
        ent["ilha_hex_mesh_bounds_size"] = ilhas[0]["hexMeshBounds"]
        ent["ilha_collider_bounds_size"] = ilhas[0]["meshColliderBounds"]
        ent["ilha_modelo_px_largura"] = stats([i["modeloPx"][0] for i in ilhas])
        ent["ilha_modelo_px_altura"] = stats([i["modeloPx"][1] for i in ilhas])
        ent["ilha_superficie_topo_px_largura"] = stats([i["modeloSuperficieTopoPx"][0] for i in ilhas])
        ent["ilha_superficie_topo_px_altura"] = stats([i["modeloSuperficieTopoPx"][1] for i in ilhas])
        ent["alturas_das_ilhas_Altura"] = stats([i["altura"] for i in ilhas])
        # decoracoes
        agg = {}
        for d in c["decoracoes"]:
            agg.setdefault(d["fonte"], []).append(d)
        ent["decoracoes_por_modelo"] = {
            k: {"contagem": len(v),
                "tamanho_normalizado": stats([x["tamanhoNormalizado"] for x in v]),
                "altura_mundo_y": stats([x["boundsMundoAABB"][1] for x in v]),
                "px_altura_AABB": stats([x["pxAABB"][1] for x in v]),
                "px_largura_AABB": stats([x["pxAABB"][0] for x in v]),
                "px_altura_OBB": stats([x["pxOBB"][1] for x in v]),
                "px_largura_OBB": stats([x["pxOBB"][0] for x in v])} for k, v in sorted(agg.items())}
        ent["decoracoes_total"] = len(c["decoracoes"])
        ent["decoracoes_px_altura_AABB_global"] = stats([d["pxAABB"][1] for d in c["decoracoes"]])
        ent["lucca"] = c["lucca"]
        ent["inimigos_medidos_temporariamente"] = c["inimigos"]
        ent["modelos_de_carta_no_jogo"] = c["modelosDeCartaNoJogo"]
        ent["camera_enquadramento"] = raws[s]["camera"]
        ent["fundo_1920x1080"] = raws[s]["fundo_1920x1080"]
        ent["fundo_2520x1080"] = raws[s]["fundo_2520x1080"]
        if s == 12345:
            ent["decoracoes_instancias"] = c["decoracoes"]
            ent["ilhas_detalhe"] = ilhas
        out["por_seed"][str(s)] = ent

    with open(os.path.join(A, "medidas.json"), "w", encoding="utf-8") as f:
        json.dump(out, f, indent=1, ensure_ascii=False)

    # ---------------- MEDIDAS.md ----------------
    P = md.append
    P("# Medidas do HexPrototipo (baseline visual)\n")
    P("Fonte: Unity Editor 6000.3.10f1, Play Mode, cena `Assets/Scenes/HexPrototipo.unity`. **Valores do Editor, nao de build.** "
      "Dados completos em `medidas.json`; reproducao em `tools/visual-baseline/`. Unidade de mundo = 1 m. px = pixels em 1920x1080 com a camera enquadrada.\n")
    P("## 1. Hierarquia da cena (Edit Mode, 28 raizes)\n")
    P("| Raiz | Ativo | Filhos | Componentes |\n|---|---|---|---|")
    for h in hier:
        P("| %s | %s | %d | %s |" % (h["name"], "sim" if h["activeSelf"] else "nao", h["childCount"], ", ".join(h["componentTypes"][1:] or h["componentTypes"])))
    P("\nObservacoes: Lucca e primitivo (2 filhos `Corpo`+`Frente`, ver 4); raizes inativas (Cube, Plane, QuadCloud x6, IlhaFlutuante, SmoothSmoke) nao renderizam. As ilhas, decoracoes, NavMesh e inimigos sao gerados em runtime (nao existem na hierarquia de Edit Mode).\n")
    P("## 2. Modelos brutos (sem escala; Edit Mode, lidos dos assets)\n")
    P("`VisualDeIlha.Criar` normaliza por bounds de **MeshFilter apenas** e reseta a escala local da raiz do modelo para 1 antes de medir.\n")
    P("| Asset | MeshFilter | SkinnedMR | Escala da raiz | Bounds cru (x,y,z) | min.y | Pivo vertical | Tris |\n|---|---|---|---|---|---|---|---|")
    for p in prefabs:
        if "erro" in p: P("| %s | erro | | | | | | |" % p["path"]); continue
        b = p["boundsCru"]
        P("| %s | %s | %s | %s | %s | %s | %s | %d |" % (p["path"].replace("Assets/Modelos/", ""), "sim" if p["usaMeshFilter"] else "nao", "sim" if p["usaSkinnedMeshRenderer"] else "nao",
          "x".join(str(r(x, 2)) for x in p["rootScale"][:1]), " x ".join(str(r(x)) for x in b["size"]), r(b["min"][1]), p.get("pivoVerticalRelativoAoBounds", "-"), p["triangulos"]))
    P("\nNotas: para `ArvoreGrande.fbx` e `Morcegp.fbx` a raiz tem escala 100 e o bounds acima e o da malha sem a escala da raiz; com escala 100 o tamanho seria ~100x maior (ArvoreGrande ~1.9 x 1.8 x 2.3). "
      "`VisualDeIlha` ignora essa escala (reseta para 1), entao so a normalizacao final importa. `Lucca.prefab` tem SkinnedMeshRenderer (bounds de localBounds), mas **nao e usado** na cena (Lucca da cena e capsula). "
      "`Pedra`, `TroncoDeMadeira`, `Coracao`, `Flecha`, `Moeda` tem pivo no centro/fora da base ('outro'); `VisualDeIlha` recentra em XZ e poe a base em y=0.\n")

    for s in SEEDS:
        e = out["por_seed"][str(s)]
        c = raws[s]["cena"]
        P("## 3.%d Seed %d\n" % (SEEDS.index(s) + 1, s))
        P("- Ilhas: %d; orthoSize 1080p = %s; escala da tela = %s px por unidade de mundo (ortografica)." % (e["n_ilhas"], r(e["orthoSize_1080p"]), r(e["px_por_unidade_mundo"], 2)))
        P("- Modelo da ilha (mundo): %s; topo do modelo relativo ao tile (min/max) = %s / %s; topo do collider relativo ao tile = %s." % (
            " x ".join(str(r(x)) for x in e["ilha_modelo_bounds_size"]), r(e["ilha_topo_y_relativo_ao_tile"]["min"]), r(e["ilha_topo_y_relativo_ao_tile"]["max"]), r(e["ilha_collider_topo_y_relativo"]["max"])))
        P("- Hex mesh (renderer desligado) bounds %s; MeshCollider bounds %s." % (" x ".join(str(r(x)) for x in e["ilha_hex_mesh_bounds_size"]), " x ".join(str(r(x)) for x in e["ilha_collider_bounds_size"])))
        P("- **Ilha em px (1080p)**: bounds do modelo inteiro (com a parte pendurada) %s x %s px (mediana, AABB de tela); superficie do topo %s x %s px (mediana)." % (
            r(e["ilha_modelo_px_largura"]["mediana"], 1), r(e["ilha_modelo_px_altura"]["mediana"], 1), r(e["ilha_superficie_topo_px_largura"]["mediana"], 1), r(e["ilha_superficie_topo_px_altura"]["mediana"], 1)))
        P("- Alturas Altura dos tiles: min %s, max %s." % (r(e["alturas_das_ilhas_Altura"]["min"]), r(e["alturas_das_ilhas_Altura"]["max"])))
        P("\n**Decoracoes** (%d instancias; `tamanho` = maior eixo local apos normalizacao; px = AABB de tela dos bounds de mundo / OBB = caixa orientada)\n" % e["decoracoes_total"])
        P("| Modelo | N | tamanho min/med/max | altura mundo min/med/max | px alt AABB min/med/max | px larg AABB med | px alt OBB med |\n|---|---|---|---|---|---|---|")
        for k, v in e["decoracoes_por_modelo"].items():
            f = lambda d, n=3: "%s / %s / %s" % (r(d["min"], n), r(d["mediana"], n), r(d["max"], n))
            P("| %s | %d | %s | %s | %s | %s | %s |" % (k, v["contagem"], f(v["tamanho_normalizado"]), f(v["altura_mundo_y"]), f(v["px_altura_AABB"], 1), r(v["px_largura_AABB"]["mediana"], 1), r(v["px_altura_OBB"]["mediana"], 1)))
        l = c["lucca"]
        P("\n**Lucca** (HexPlayer): bounds de mundo %s (Corpo MeshRenderer cilindro/capsula + Frente), base em y=%s do pivo, **%s x %s px (L x A)** em 1080p. Agente NavMesh raio %s, altura %s." % (
            " x ".join(str(r(x)) for x in l["boundsUniao"]["size"]), r(l["boundsUniao"]["baseRelativaAoPivo"]), r(l["px"][0], 1), r(l["px"][1], 1), l.get("agenteRaio"), l.get("agenteAltura")))
        for i in c["inimigos"]:
            if "boundsSize" in i:
                P("- Inimigo %s (spawnado temporariamente via `InimigoSpawner.Nascer`, escala def %s, prefab %s, usa SkinnedMR: %s): bounds %s, **%s x %s px**%s." % (
                    i["def"], i["escalaDef"], i["prefab"], i["usaSkinned"], " x ".join(str(r(x)) for x in i["boundsSize"]), r(i["px"][0], 1), r(i["px"][1], 1),
                    (", altura acima do topo do tile %s" % r(i["alturaAcimaDoTopoDoTile"])) if "alturaAcimaDoTopoDoTile" in i else ""))
        P("- Cartas com `modelo` (aplicadas como `Plataforma.Montar`, instancia temporaria sobre o acampamento; nada colocado de fato):")
        for pm in c["modelosDeCartaNoJogo"]:
            if "boundsSize" in pm:
                P("  - %s (prefab %s, tamanhoDoModelo %s, altura %s): bounds %s, %s x %s px." % (pm["carta"], pm["prefab"], pm["tamanhoDoModelo"], pm["alturaDoModelo"], " x ".join(str(r(x)) for x in pm["boundsSize"]), r(pm["px"][0], 1), r(pm["px"][1], 1)))
        P("")

    P("## 4. Camera: orthoSize com e sem decoracoes (formula de `HexIsoCamera.MedirIlhas` replicada)\n")
    P("A = todos os renderers filhos das ilhas (formula real; confere com o orthoSize do Editor), B = sem `Decoracoes`, C = so modelo da ilha, D = so MeshRenderer do hexagono (desligado), E = so decoracoes. margem=%s (cena), folga=tamanhoDoHexagono=3.\n" % raws[12345]["camera"]["aspectos"][0]["margem"])
    P("| Seed | Aspecto | orthoSize Editor | A | B (sem deco) | delta A vs B | C | D | E (so deco) | Limitado por |\n|---|---|---|---|---|---|---|---|---|---|")
    for s in SEEDS:
        for a in raws[s]["camera"]["aspectos"]:
            A_, B_ = a["A_todos_renderers"]["orthoSize"], a["B_sem_decoracoes"]["orthoSize"]
            P("| %d | %s | %s | %s | %s | %s%% | %s | %s | %s | %s |" % (s, a["resolucao"], r(a["orthoSizeReal_Enquadrar"]), r(A_), r(B_), r(100 * (A_ - B_) / B_, 2),
              r(a["C_so_modelo_da_ilha"]["orthoSize"]), r(a["D_so_hex_mesh"]["orthoSize"]), r(a["E_so_decoracoes"]["orthoSize"]), a["A_todos_renderers"]["limitadoPor"]))
    P("\nLeitura numerica: nas tres seeds os bounds das decoracoes nao alteram o enquadramento (delta 0%) porque o modelo da ilha (com a parte pendurada, ~3.45 m abaixo do topo) ja define os extremos. "
      "Considerar so o mesh do hexagono (D) daria orthoSize menor, mostrando quanto o modelo da ilha pesa. Em 21:9 o orthoSize e igual ao de 16:9 (limitado pela altura), so a largura visivel cresce.\n")

    P("## 5. Fundo / ambiente (seed 12345, 1920x1080)\n")
    b = raws[12345]["fundo_1920x1080"]
    P("- Anel de ilhas: centro %s, raio max centro-a-centro XZ %s, Y das ilhas de %s (base pendurada) a %s (topo, decoracoes); profundidade na camera (z camera-espaco) dos renderers de %s a %s." % (
        [r(x, 2) for x in b["anel"]["centro"]], r(b["anel"]["raioMaxIlhasCentroXZ"], 2), r(b["anel"]["yInferiorDasIlhas"], 2), r(b["anel"]["ySuperiorDasIlhas"], 2),
        r(b["anel"]["profundidadeCamZ_centroDosRenderers_min"], 1), r(b["anel"]["profundidadeCamZ_max"], 1)))
    ob = {o["objeto"]: o for o in b["objetos"]}
    for n, o in ob.items():
        P("- **%s**: posicao %s, escala mundo %s, bounds %s (min %s, max %s), topo Y %s = %s m abaixo da base das ilhas, profundidade camera-z do centro %s (anel %s a %s), distancia XZ ao centro do anel %s." % (
            n, [r(x, 2) for x in o["posMundo"]], [r(x, 2) for x in o["escalaMundo"]], [r(x, 1) for x in o["boundsSize"]], [r(x, 1) for x in o["boundsMin"]], [r(x, 1) for x in o["boundsMax"]],
            r(o["boundsMax"][1], 2), r(b["anel"]["yInferiorDasIlhas"] - o["boundsMax"][1], 2), r(o["profundidadeCamZ_centro"], 1), r(b["anel"]["profundidadeCamZ_centroDosRenderers_min"], 1), r(b["anel"]["profundidadeCamZ_max"], 1), r(o["distanciaXZ_centroParaCentroDoAnel"], 1)))
    P("- `CenarioContinuo.margem` = %s; `agua` nao atribuida (o rio tem seu proprio `FundoContinuo`)." % b["cenarioComponente"]["margem"])
    # grupos do cenario
    grp = {}
    for ch in b["cenarioFilhos"]:
        if "boundsSize" not in ch: continue
        grp.setdefault(bg_group(ch["nome"]), []).append(ch)
    ringmin, ringmax = b["anel"]["profundidadeCamZ_centroDosRenderers_min"], b["anel"]["profundidadeCamZ_max"]
    ringylow = b["anel"]["yInferiorDasIlhas"]
    P("\nFilhos do cenario por tipo (61 filhos): N, altura de mundo tipica, Y max, camera-z min/max (comparar com anel %s..%s), quantos tem centro de tela no viewport 1920x1080:\n" % (r(ringmin, 1), r(ringmax, 1)))
    P("| Tipo | N | altura bounds med | yMax min/max | cam-z min/max | mais perto da camera que o anel | no viewport |\n|---|---|---|---|---|---|---|")
    for k, v in sorted(grp.items()):
        inview = 0
        for x in v:
            cx = (x["telaPx_min"][0] + x["telaPx_max"][0]) / 2; cy = (x["telaPx_min"][1] + x["telaPx_max"][1]) / 2
            if 0 <= cx <= 1920 and 0 <= cy <= 1080: inview += 1
        P("| %s | %d | %s | %s / %s | %s / %s | %d | %d |" % (k, len(v), r(st.median([x["boundsSize"][1] for x in v]), 2), r(min(x["yMax"] for x in v), 1), r(max(x["yMax"] for x in v), 1),
          r(min(x["camZ"] for x in v), 1), r(max(x["camZ"] for x in v), 1), sum(1 for x in v if x["camZ"] < ringmin), inview))
    allc = [x for v in grp.values() for x in v]
    P("\nNenhum filho do cenario tem yMax acima de %s (base das ilhas)?  %s (maior yMax do cenario: %s)." % (r(ringylow, 2), "sim" if max(x["yMax"] for x in allc) < ringylow else "nao", r(max(x["yMax"] for x in allc), 2)))
    P("\n**Rio** (seeds): superficie Y e vertice mais perto de um centro de ilha (distancia XZ):")
    for s in SEEDS:
        rr = raws[s]["fundo_1920x1080"]["rio"]
        P("- seed %d: superficie Y %s (base das ilhas %s => rio %s m abaixo), vertice mais perto a %s m em XZ do centro da ilha `%s` (raio visual da ilha 2.1), %d vertices; ilhas dentro do AABB XZ do rio: %d." % (
            s, r(rr["yDaSuperficie"], 2), r(rr["yInferiorDasIlhas"], 2), r(rr["yInferiorDasIlhas"] - rr["yDaSuperficie"], 2), r(rr["distanciaXZ_verticeMaisPertoDeCentroDeIlha"], 2), rr["ilhaMaisPerto"], rr["vertices"], raws[s]["fundo_1920x1080"]["rioIlhasDentroDoAABBxz"]))
    nv = b["nuvens"]
    P("\n**Nuvens** (CamadaDeNuvens, %d): Y de %s a %s (base das ilhas %s; folga %s m abaixo das ilhas), camera-z %s a %s; %d atras do anel em profundidade, %d na frente." % (
        nv["quantidade"], r(nv["yMin"], 2), r(nv["yMax"], 2), r(nv["yInferiorDasIlhas"], 2), r(nv["abaixoDoAnel_distancia"], 2), r(nv["camZ_min"], 1), r(nv["camZ_max"], 1), nv["atrasEmProfundidadeDoAnel"], nv["frenteEmProfundidadeDoAnel"]))
    P("\nO fundo e reposicionado/reescalado todo frame (`CenarioContinuo`, `FundoContinuo`, nuvens ancoradas na camera): posicoes acima valem para este enquadramento/aspecto (os arquivos de 21:9 estao em `medidas.json` > `fundo_2520x1080`).\n")

    P("## 6. Iluminacao por hora (seed 12345; DayNightController, sunrise 6 / sunset 18)\n")
    P("| Hora | Sol rot (euler) | Sol cor RGB | Intens. | Ambient (modo) sky / equator / ground | Fog (modo, dens., cor) | Camera clear / bg |\n|---|---|---|---|---|---|---|")
    fc = lambda c: "(%s, %s, %s)" % (r(c[0], 2), r(c[1], 2), r(c[2], 2))
    for x in [l for l in light if l["seed"] == 12345]:
        P("| %s (%s) | %s | %s | %s | %s: %s / %s / %s | %s, %s, %s | %s / %s |" % (x["timeOfDay"], x["rotulo"], [r(v, 1) for v in x["sol"]["rotEuler"]], fc(x["sol"]["cor"]), r(x["sol"]["intensidade"], 3),
          x["ambiente"]["modo"], fc(x["ambiente"]["sky"]), fc(x["ambiente"]["equator"]), fc(x["ambiente"]["ground"]), x["fog"]["modo"], x["fog"]["densidade"], fc(x["fog"]["cor"]), x["camera"]["clearFlags"], fc(x["camera"]["background"])))
    x0 = light[0]
    P("\nSombras do sol: %s, forca %s. Camera URP: post-processing %s, antialiasing %s, HDR %s; skybox nenhum (SolidColor). Fog mode Exponential: start/end nao usados (valores %s / %s). O fog tem a cor do ceu/ambient em todas as horas (gerado por `DayNightController`)." % (
        x0["sol"]["sombras"], x0["sol"]["forcaSombra"], x0["cameraURP"]["renderPostProcessing"], x0["cameraURP"]["antialiasing"], x0["camera"]["allowHDR"], x0["fog"]["inicio"], x0["fog"]["fim"]))
    v = x0["volumes"][0]
    P("\nVolume: `%s` (global=%s, peso %s), perfil `%s`. Overrides ativos:" % (v["objeto"], v["global"], v["peso"], v["perfil"]))
    for comp in v["componentes"]:
        ov = {k.replace(" [override]", ""): val for k, val in comp["parametros"].items() if "[override]" in k}
        P("- %s (ativo=%s): %s" % (comp["tipo"], comp["ativo"], ov or "nenhum override"))
    P("\nA mesma configuracao de Volume vale para todas as horas (o ciclo so altera sol, ambient, fog e cor de fundo).\n")

    P("## 7. Estatisticas de render (EDITOR, nao build)\n")
    m = stt["mediana"]
    P("Seed 12345, 12h, Main Camera desenhando em RenderTexture 1920x1080 (`UnityStats.screenRes` reporta %s, o tamanho da Game view); mediana de 12 amostras (0.5 s) de `UnityEngine.UnityStats`: " % stt["amostras_UnityStats"][0]["screenRes"] +
      "batches %s, setPassCalls %s, drawCalls %s, triangulos %s, vertices %s, shadowCasters %s." % (m["batches"], m["setPassCalls"], m["drawCalls"], f"{m['triangles']:,}", f"{m['vertices']:,}", m["shadowCasters"]))
    P("Tempo medio de frame do Editor: %s ms (%s fps) sobre %d frames em %s s (frames contados por `Time.frameCount`, inclui overhead do Editor, timeScale 0; **nao** e desempenho de build)." % (r(stt["frametime_medio_ms_do_editor"], 2), r(stt["fps_medio_do_editor"], 1), stt["frames_no_intervalo"], r(stt["segundos"], 2)))
    P("Incerteza: `UnityStats` reflete o ultimo frame renderizado pelo Editor; como a Game view e a RenderTexture podem ser renderizadas no mesmo frame, os totais podem somar mais de uma passada de camera.\n")
    P("## 8. Limitacoes / nao medido\n")
    P("- Capturas por `Camera.Render()`: UI Screen Space Overlay (HUD) e IMGUI nao entram; um screenshot da Game view (472x266, tamanho do painel do Editor) com HUD esta em `docs/art/baseline/gameview_hud_s12345_h12.png`.")
    P("- Inimigos nao existem com `timeScale=0` logo apos gerar o mapa; foram spawnados temporariamente (Morcego e Lobo) so para medir. O Lobo usa `Lobo (1).fbx` (SkinnedMeshRenderer apenas): `VisualDeIlha` mede so MeshFilter, entao a escala resultante e anormal (bounds %s m) - valor real lido do Editor, nao interpretado." % " x ".join(str(r(x, 2)) for x in raws[12345]["cena"]["inimigos"][1]["boundsSize"]))
    P("- Modelos `Arvore`/`Morcego` novos (Mateus) so medidos como assets; nenhum usado na cena.")
    P("- Medidas de px usam `Camera.WorldToScreenPoint` com a camera em RenderTexture 1920x1080 e aspecto forcado; bounds de renderers desabilitados (hex mesh) nao entram nos px.")
    with open(os.path.join(A, "MEDIDAS.md"), "w", encoding="utf-8") as f:
        f.write("\n".join(md) + "\n")


if __name__ == "__main__":
    main()
