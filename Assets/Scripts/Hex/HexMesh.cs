using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Gera a malha de um hexagono "pointy-top" deitado no plano XZ.
    /// A face de cima fica em Y = 0 (e a superficie onde o player anda) e o
    /// hexagono e extrudado para baixo, virando uma laje fina.
    /// </summary>
    public static class HexMesh
    {
        public static Mesh Create(float size, float thickness)
        {
            var layout = new HexLayout(size);

            var mesh = new Mesh { name = "Hexagono" };

            // 6 vertices do topo + 1 centro, mais 6 pares para as paredes laterais.
            bool extruded = thickness > 0.0001f;

            var vertices = new Vector3[extruded ? 7 + 24 : 7];
            var triangles = new int[extruded ? 18 + 36 : 18];

            // --- Tampa superior: leque de triangulos a partir do centro ---
            vertices[0] = Vector3.zero;
            for (int i = 0; i < 6; i++)
            {
                vertices[1 + i] = layout.Corner(i);
            }

            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                // Ordem (centro, i+1, i) deixa a normal apontando para +Y.
                triangles[i * 3 + 0] = 0;
                triangles[i * 3 + 1] = 1 + next;
                triangles[i * 3 + 2] = 1 + i;
            }

            if (extruded)
            {
                // --- Paredes laterais: um quad por aresta, com vertices proprios
                // para que a normal da lateral nao suavize com a do topo. ---
                int v = 7;
                int t = 18;
                for (int i = 0; i < 6; i++)
                {
                    int next = (i + 1) % 6;
                    Vector3 topA = layout.Corner(i);
                    Vector3 topB = layout.Corner(next);
                    Vector3 bottomA = topA + Vector3.down * thickness;
                    Vector3 bottomB = topB + Vector3.down * thickness;

                    vertices[v + 0] = topA;
                    vertices[v + 1] = topB;
                    vertices[v + 2] = bottomA;
                    vertices[v + 3] = bottomB;

                    triangles[t + 0] = v + 0;
                    triangles[t + 1] = v + 1;
                    triangles[t + 2] = v + 2;

                    triangles[t + 3] = v + 1;
                    triangles[t + 4] = v + 3;
                    triangles[t + 5] = v + 2;

                    v += 4;
                    t += 6;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
