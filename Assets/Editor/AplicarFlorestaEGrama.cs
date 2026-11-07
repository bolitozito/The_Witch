using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Globalization;

// Curso do Jogo de Terror (Unity 5.6): planta no Terrain as 175 arvores e as 103 celulas de grama do jogo.
public static class AplicarFlorestaEGrama
{
    const string ARVORE = "Assets/treeCreatorTutorial/tree construction/01 sycamore trees finalized/syc_01 manual corrected.prefab";
    const string GRAMA = "Assets/Standard Assets/Environment/TerrainAssets/BillboardTextures/GrassFrond02AlbedoAlpha.psd";
    const string ARQ_ARVORES = "Assets/Editor/floresta-arvores.txt";
    const string ARQ_GRAMA = "Assets/Editor/grama-celulas.txt";
    const int DETALHE = 1024;
    const int TOTAL_ARVORES = 175;
    const int TOTAL_CELULAS = 103;

    static float F(string s)
    {
        return float.Parse(s, CultureInfo.InvariantCulture);
    }

    static void Aviso(string texto)
    {
        EditorUtility.DisplayDialog("Curso Terror", texto, "OK");
    }

    [MenuItem("Curso Terror/Aplicar floresta e grama")]
    static void Aplicar()
    {
        Terrain terreno = null;
        if (Selection.activeGameObject != null)
            terreno = Selection.activeGameObject.GetComponent<Terrain>();
        if (terreno == null)
            terreno = Terrain.activeTerrain;
        if (terreno == null)
        {
            Aviso("Nao achei nenhum Terrain na cena. Selecione o Terrain na Hierarchy e tente de novo.");
            return;
        }

        TerrainData dados = terreno.terrainData;
        if (dados.detailResolution != DETALHE)
        {
            Aviso("A Detail Resolution do Terrain precisa ser " + DETALHE + ". Agora ela e " + dados.detailResolution + ".");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath(ARVORE, typeof(GameObject)) as GameObject;
        if (prefab == null)
        {
            Aviso("Nao achei a arvore syc_01 manual corrected. Importe o pacote da Aula 2 de novo.");
            return;
        }
        Texture2D textura = AssetDatabase.LoadAssetAtPath(GRAMA, typeof(Texture2D)) as Texture2D;
        if (textura == null)
        {
            Aviso("Nao achei a textura GrassFrond02AlbedoAlpha (Standard Assets > Environment > TerrainAssets > BillboardTextures). Importe os Standard Assets da Aula 0.");
            return;
        }
        TextAsset arqArvores = AssetDatabase.LoadAssetAtPath(ARQ_ARVORES, typeof(TextAsset)) as TextAsset;
        TextAsset arqGrama = AssetDatabase.LoadAssetAtPath(ARQ_GRAMA, typeof(TextAsset)) as TextAsset;
        if (arqArvores == null || arqGrama == null)
        {
            Aviso("Nao achei os arquivos de dados da pasta Editor. Importe o pacote da Aula 2 de novo.");
            return;
        }

        char[] quebras = new char[] { '\n', '\r' };
        string[] linhasArvores = arqArvores.text.Split(quebras, System.StringSplitOptions.RemoveEmptyEntries);
        string[] linhasGrama = arqGrama.text.Split(quebras, System.StringSplitOptions.RemoveEmptyEntries);
        if (linhasArvores.Length != TOTAL_ARVORES || linhasGrama.Length != TOTAL_CELULAS)
        {
            Aviso("Os arquivos de dados estao com o tamanho errado (" + linhasArvores.Length + " arvores e " + linhasGrama.Length + " celulas de grama). Importe o pacote da Aula 2 de novo.");
            return;
        }

        // Arvores
        TreeInstance[] arvores = new TreeInstance[TOTAL_ARVORES];
        for (int i = 0; i < TOTAL_ARVORES; i++)
        {
            string[] c = linhasArvores[i].Split(';');
            string[] cor = c[6].Split(',');
            TreeInstance t = new TreeInstance();
            t.position = new Vector3(F(c[0]), F(c[1]), F(c[2]));
            t.widthScale = F(c[3]);
            t.heightScale = F(c[4]);
            t.rotation = F(c[5]);
            t.color = new Color32(byte.Parse(cor[0]), byte.Parse(cor[1]), byte.Parse(cor[2]), byte.Parse(cor[3]));
            t.lightmapColor = new Color32(255, 255, 255, 255);
            t.prototypeIndex = int.Parse(c[7]);
            arvores[i] = t;
        }

        // Grama
        int[,] mapa = new int[DETALHE, DETALHE];
        for (int i = 0; i < TOTAL_CELULAS; i++)
        {
            string[] c = linhasGrama[i].Split(',');
            mapa[int.Parse(c[0]), int.Parse(c[1])] = int.Parse(c[2]);
        }

        Undo.RegisterCompleteObjectUndo(dados, "Aplicar floresta e grama");
        Undo.RegisterCompleteObjectUndo(terreno, "Aplicar floresta e grama");

        dados.treeInstances = new TreeInstance[0];
        TreePrototype tp = new TreePrototype();
        tp.prefab = prefab;
        tp.bendFactor = 0f;
        dados.treePrototypes = new TreePrototype[] { tp };
        dados.treeInstances = arvores;

        DetailPrototype dp = new DetailPrototype();
        dp.prototypeTexture = textura;
        dp.usePrototypeMesh = false;
        dp.renderMode = DetailRenderMode.GrassBillboard;
        dp.minWidth = 1f;
        dp.maxWidth = 2f;
        dp.minHeight = 1f;
        dp.maxHeight = 2f;
        dp.noiseSpread = 0.1f;
        dp.bendFactor = 0.1f;
        dp.healthyColor = new Color32(67, 249, 42, 255);
        dp.dryColor = new Color32(205, 188, 26, 255);
        dados.detailPrototypes = new DetailPrototype[] { dp };
        dados.SetDetailLayer(0, 0, 0, mapa);

        dados.wavingGrassStrength = 0.5f;
        dados.wavingGrassAmount = 0.5f;
        dados.wavingGrassSpeed = 0.5f;
        dados.wavingGrassTint = new Color(0.7f, 0.6f, 0.5f, 0f);

        terreno.treeDistance = 5000f;
        terreno.treeBillboardDistance = 50f;
        terreno.detailObjectDistance = 80f;
        terreno.detailObjectDensity = 1f;

        EditorUtility.SetDirty(dados);
        EditorUtility.SetDirty(terreno);
        EditorSceneManager.MarkSceneDirty(terreno.gameObject.scene);
        terreno.Flush();
        Debug.Log("Curso Terror: floresta e grama aplicadas no Terrain (" + dados.treeInstanceCount + " arvores e " + TOTAL_CELULAS + " celulas de grama).");
    }
}
