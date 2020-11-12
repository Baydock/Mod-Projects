using Assets.Main.Scenes;
using Harmony;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BTD6ButItsNot {

    public class BTD6ButItsNotMod : MelonMod { }

    [HarmonyPatch(typeof(TitleScreen), "OnPlayButtonClicked")]
    public class PlayButtonClicked_Patch {
        [HarmonyPrefix]
        public static bool Prefix() {

            AssetBundle questionMark = AssetBundle.LoadFromMemory(QuestionMark.bytes);
            Scene scene = SceneManager.LoadScene(Path.GetFileNameWithoutExtension(questionMark.GetAllScenePaths()[0]), new LoadSceneParameters { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.None });

            OnSceneLoaded(scene);

            return false;
        }

        private static readonly int width = 10;
        private static readonly int height = 20;
        private static readonly Color emptyColor = Color.black;
        private static float fallFrequency = .8f;
        private static readonly bool[,] allBoxes = new bool[width, height + 1];
        private static readonly Color[,] boxColors = new Color[width, height + 1];
        private static readonly GameObject[,] gameBoxes = new GameObject[width, height + 1];
        private static float prevFall;
        private static Tetrino current;

        private static async void OnSceneLoaded(Scene scene) {
            while (!scene.isLoaded)
                await Task.Delay(25);

            GameObject board = scene.GetRootGameObjects().FirstOrDefault(g => g.name.Contains("Board"));

            GameObject boxPrefab = new GameObject("Square");
            SpriteRenderer r = boxPrefab.AddComponent<SpriteRenderer>();
            r.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(), 4);
            boxPrefab.transform.localScale = new Vector3(.95f, .95f, 1);

            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    GameObject box = UnityEngine.Object.Instantiate(boxPrefab);
                    box.transform.parent = board.transform;
                    box.GetComponent<SpriteRenderer>().color = emptyColor;
                    SetBoxPosition(new Vector3Int(x, y, 0), box);
                    gameBoxes[x, y] = box;
                    boxColors[x, y] = emptyColor;
                }
            }

            current = new Tetrino(GetRandomTetrinoType());

            Timer timer = new Timer(1000d / 60d);
            timer.Elapsed += (s, e) => {
                bool left = Input.GetKeyDown(KeyCode.LeftArrow), right = Input.GetKeyDown(KeyCode.RightArrow);
                if (left != right) {
                    if (left)
                        current.TryMove(Vector3Int.left);
                    else
                        current.TryMove(Vector3Int.right);
                }

                if (Input.GetKeyDown(KeyCode.A))
                    current.TryRotate(-90);
                if (Input.GetKeyDown(KeyCode.D))
                    current.TryRotate(90);
                if (Input.GetKeyDown(KeyCode.W))
                    current.TryRotate(180);

                if (Time.time - prevFall > (Input.GetKey(KeyCode.DownArrow) ? fallFrequency / 10 : fallFrequency)) {
                    if (current.TryMove(Vector3Int.down, true))
                        current = new Tetrino(GetRandomTetrinoType());
                    else
                        prevFall = Time.time;
                }
            };
            timer.Start();
        }
        private static TetrinoType GetRandomTetrinoType() {
            Array types = Enum.GetValues(typeof(TetrinoType));
            return (TetrinoType)types.GetValue(UnityEngine.Random.Range(0, types.Length));
        }

        private static void SetBoxPosition(Vector3Int pos, GameObject gameBox) => gameBox.transform.localPosition = pos + new Vector3(.5f, .5f);

        private class Tetrino {
            private static readonly Dictionary<TetrinoType, Color> colors = new Dictionary<TetrinoType, Color> {
            { TetrinoType.I, Color.cyan },
            { TetrinoType.J, Color.blue },
            { TetrinoType.L, new Color(1, .5f, 0) },
            { TetrinoType.O, Color.yellow },
            { TetrinoType.S, Color.green },
            { TetrinoType.T, Color.magenta },
            { TetrinoType.Z, Color.red }
        }; private static readonly Dictionary<TetrinoType, bool> evens = new Dictionary<TetrinoType, bool> {
            { TetrinoType.I, true },
            { TetrinoType.J, false },
            { TetrinoType.L, false },
            { TetrinoType.O, true },
            { TetrinoType.S, false },
            { TetrinoType.T, false },
            { TetrinoType.Z, false }
        };
            private Vector3Int[] boxes;
            private Vector3Int center;
            private Color color;
            private bool almostDead = false;
            private bool even;

            public Tetrino(TetrinoType type) {
                switch (type) {
                    case TetrinoType.I:
                        boxes = new Vector3Int[] { new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0) };
                        break;
                    case TetrinoType.J:
                        boxes = new Vector3Int[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(1, -1, 0) };
                        break;
                    case TetrinoType.L:
                        boxes = new Vector3Int[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(1, 1, 0) };
                        break;
                    case TetrinoType.O:
                        boxes = new Vector3Int[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0) };
                        break;
                    case TetrinoType.S:
                        boxes = new Vector3Int[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0) };
                        break;
                    case TetrinoType.T:
                        boxes = new Vector3Int[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0) };
                        break;
                    case TetrinoType.Z:
                        boxes = new Vector3Int[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, 1, 0), new Vector3Int(0, 1, 0) };
                        break;
                }
                color = colors[type];
                even = evens[type];
                center = new Vector3Int(4, 18, 0);

                for (int i = 0; i < boxes.Length; i++)
                    boxes[i] += center;
            }

            public bool TryMove(Vector3Int move, bool canKill = false) {
                if (almostDead && canKill)
                    return true;
                almostDead = false;
                Vector3Int[] moveTo = new Vector3Int[boxes.Length];
                for (int i = 0; i < boxes.Length; i++)
                    moveTo[i] = boxes[i] + move;
                if (CheckCollision(moveTo)) {
                    almostDead = canKill;
                } else {
                    MoveBoxes(moveTo);
                    center += move;
                }
                return false;
            }

            public void TryRotate(float degrees) {
                Vector3Int[] rotateTo = new Vector3Int[boxes.Length];
                for (int i = 0; i < boxes.Length; i++)
                    rotateTo[i] = RotateBox(boxes[i], degrees);
                if (!CheckCollision(rotateTo))
                    MoveBoxes(rotateTo);
            }

            private Vector3Int RotateBox(Vector3Int toRotate, double angleInDegrees) {
                double angleInRadians = angleInDegrees * (Math.PI / 180);
                double cosTheta = Math.Cos(angleInRadians);
                double sinTheta = Math.Sin(angleInRadians);
                Vector3 trueCenter = center;
                if (even) trueCenter += new Vector3(.5f, .5f);
                double difX = toRotate.x - trueCenter.x;
                double difY = toRotate.y - center.y;
                return new Vector3Int(
                    (int)Math.Round(cosTheta * difX - sinTheta * difY + trueCenter.x),
                    (int)Math.Round(sinTheta * difX + cosTheta * difY + trueCenter.y), 0
                );
            }

            private bool CheckCollision(Vector3Int[] to) {
                for (int i = 0; i < to.Length; i++)
                    if (!boxes.Contains(to[i]) && (to[i].x < 0 || to[i].x >= width || to[i].y < 0 || allBoxes[to[i].x, to[i].y]))
                        return true;
                return false;
            }

            private void MoveBoxes(Vector3Int[] to) {
                ColorSpaces(boxes, false);
                ColorSpaces(to, true);
                boxes = to;
            }

            private void ColorSpaces(Vector3Int[] toColor, bool toggle) {
                for (int i = 0; i < toColor.Length; i++) {
                    allBoxes[toColor[i].x, toColor[i].y] = toggle;
                    gameBoxes[toColor[i].x, toColor[i].y].GetComponent<SpriteRenderer>().color = toggle ? color : emptyColor;
                }
            }
        }

        private enum TetrinoType { I, J, L, O, S, T, Z }
    }
}