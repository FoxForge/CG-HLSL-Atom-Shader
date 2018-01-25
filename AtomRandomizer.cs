using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System;

[RequireComponent(typeof(Renderer))]
public class AtomRandomizer : MonoBehaviour
{
    public Color32 ForegroundColor
    {
        get { return _foregroundColor; }
        set { _foregroundColor = value; }
    }

    public Color32 BackgroundColor
    {
        get { return _backgroundColor; }
        set { _backgroundColor = value; }
    }

    public bool RandomizeForeground
    {
        get { return _randomizeForeground; }
        set { _randomizeForeground = value; }
    }

    public bool RandomizeBackground
    {
        get { return _randomizeBackground; }
        set { _randomizeBackground = value; }
    }

    public bool RandomizeAtomFields
    {
        get { return _randomizeAtomFields; }
        set { _randomizeAtomFields = value; }
    }

    public bool StressTest
    {
        get { return _stressTest; }
        set { _stressTest = value; }
    }

    public float RandomFactor
    {
        get { return _randomFactor; }
        set { _randomFactor = value; }
    }

    private bool _randomizeForeground = true;
    private bool _randomizeBackground = false;
    private Color32 _foregroundColor = Color.red;
    private Color32 _backgroundColor = Color.black;
    private bool _randomizeAtomFields = true;
    private float _randomFactor = 0.7f;
    private bool _stressTest;

    private Renderer _renderer;

    private const float RADIAL_MOD_MAX = 120.0F;
    private const float NUCLEUS_ATTR_MAX = 10F;
    private const float NUCLEUS_REPL_MAX = 0.5F;
    private const float NUCLEUS_SIZE_MAX = 45.0F;
    private const float ELECTR_COUNT_MAX = 150F;
    private const float ELECTR_COUNT_STRESS = 20000.0F;
    private const float ELECTR_SIZE_MAX = 8.0F;
    private const float ELECTR_SPEED_MAX = 17.0F;
    private const float RADIAL_MAX = 120F;

    private const float ATOM_STATE_TIME = 10F;
    private const float STRESS_ADDITION = 3000F;

    private const int FRAME_SAFETY = 7;
    private bool _invokeRandom = true;

    // Use this for initialization
    private void Start ()
    {
        _renderer = GetComponent<Renderer>();
        StartCoroutine(AtomShaderCoroutine());
    }

    private bool EqualColor(Color32 color1, Color32 color2)
    {
        return (color1.r == color2.r &&
            color1.g == color2.g &&
            color1.b == color2.b &&
            color1.a == color2.a);
    }

    private struct RandomAtom
    {
        public float nucleusAttraction;
        public float nucleusRepulsion;
        public float nucleusSize;
        public float electronCount;
        public float electronSize;
        public float electronSpeed;

        public float radialModifier;

        public RandomAtom(
            float nucleusAttraction,
            float nucleusRepulsion,
            float nucleusSize,
            float electronCount,
            float electronSize,
            float electronSpeed,
            float radialModifier)
        {
            this.nucleusAttraction  = nucleusAttraction;
            this.nucleusRepulsion   = nucleusRepulsion;
            this.nucleusSize        = nucleusSize;
            this.electronCount      = electronCount;
            this.electronSize       = electronSize;
            this.electronSpeed      = electronSpeed;
            this.radialModifier     = radialModifier;
        }
    }
	
    private IEnumerator AtomShaderCoroutine()
    {
        _renderer.material.SetColor("_ForegroundColor", _foregroundColor);
        _renderer.material.SetColor("_BackgroundColor", _backgroundColor);

        _renderer.material.SetFloat("_RandomForegroundColor", (_randomizeForeground) ? 1f : 0f);
        _renderer.material.SetFloat("_RandomBackgroundColor", (_randomizeBackground) ? 1f : 0f);

        bool prevForegroundSet = _randomizeForeground;
        bool prevBackgroundSet = _randomizeBackground;

        Color32 prevForeColor = _foregroundColor;
        Color32 prevBackColor = _backgroundColor;

        float stateTime = 0.0f;

        RandomAtom nextAtom = new RandomAtom();
        RandomAtom prevRandomAtom = new RandomAtom
        (
            _renderer.material.GetFloat("_NucleusAttraction"),
            _renderer.material.GetFloat("_NucleusRepulsion"),
            _renderer.material.GetFloat("_NucleusSize"),
            _renderer.material.GetFloat("_ElectronCount"),
            _renderer.material.GetFloat("_ElectronSize"),
            _renderer.material.GetFloat("_ElectronSpeed"),
            _renderer.material.GetFloat("_RadialModifier")
        );

        while (true)
        {
            if (prevForegroundSet != _randomizeForeground)
            {
                _renderer.material.SetFloat("_RandomForegroundColor", (_randomizeForeground) ? 1f : 0f);
                prevForegroundSet = _randomizeForeground;
            }

            if (prevBackgroundSet != _randomizeBackground)
            {
                _renderer.material.SetFloat("_RandomBackgroundColor", (_randomizeBackground) ? 1f : 0f);
                prevBackgroundSet = _randomizeBackground;
            }

            if (!EqualColor(prevForeColor, _foregroundColor))
            {
                _renderer.material.SetColor("_ForegroundColor", _foregroundColor);
                prevForeColor = _foregroundColor;
            }

            if (!EqualColor(prevBackColor, _backgroundColor))
            {
                _renderer.material.SetColor("_BackgroundColor", _backgroundColor);
                prevBackColor = _backgroundColor;
            }

            if (!_randomizeAtomFields)
            {
                yield return null;
                continue;
            }

            if (_invokeRandom)
            {
                nextAtom = GenerateRandomAtom(prevRandomAtom);
                _invokeRandom = false;
                yield return null;
                continue;
            }
            else
            {
                if (LerpAtomFields(ref prevRandomAtom, nextAtom, Time.deltaTime))
                {
                    SetAtomShaderProperties(prevRandomAtom);
                    yield return new WaitForSeconds(Time.deltaTime);
                    continue;
                }
                else
                {
                    // Hold time in this random state
                    if (stateTime < ATOM_STATE_TIME)
                    {
                        yield return new WaitForSeconds(Time.deltaTime);
                        stateTime += Time.deltaTime;
                        continue;
                    }
                    else
                    {
                        stateTime = 0.0f;
                        _invokeRandom = true;
                        yield return null;
                        continue;
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _invokeRandom = true;
            Debug.Log("Randomizing...");
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            _randomizeAtomFields = !_randomizeAtomFields;
            Debug.Log("Toggled Randomizer");
        }
    }

    private void SetAtomShaderProperties(RandomAtom current)
    {
        _renderer.material.SetFloat("_NucleusAttraction",   current.nucleusAttraction);
        _renderer.material.SetFloat("_NucleusRepulsion",    current.nucleusRepulsion);
        _renderer.material.SetFloat("_NucleusSize",         current.nucleusSize);
        _renderer.material.SetFloat("_ElectronCount",       current.electronCount);
        _renderer.material.SetFloat("_ElectronSize",        current.electronSize);
        _renderer.material.SetFloat("_ElectronSpeed",       current.electronSpeed);
        _renderer.material.SetFloat("_RadialModifier",      current.radialModifier);
    }

    private bool LerpAtomFields(ref RandomAtom current, RandomAtom next, float t)
    {
        if (Mathf.Abs(current.electronSpeed - next.electronSpeed) < 1.5)
        {
            return false;
        }

        current.nucleusAttraction   = Mathf.Lerp(current.nucleusAttraction, next.nucleusAttraction, t);
        current.nucleusRepulsion    = Mathf.Lerp(current.nucleusRepulsion,  next.nucleusRepulsion,  t);
        current.nucleusSize         = Mathf.Lerp(current.nucleusSize,       next.nucleusSize,       t);
        current.electronCount       = Mathf.Lerp(current.electronCount,     next.electronCount,     t);
        current.electronSize        = Mathf.Lerp(current.electronSize,      next.electronSize,      t);
        current.electronSpeed       = Mathf.Lerp(current.electronSpeed,     next.electronSpeed,     t);
        current.radialModifier      = Mathf.Lerp(current.radialModifier,    next.radialModifier,    t);
        return true;
    }


    RandomAtom GenerateRandomAtom(RandomAtom previous)
    {
        RandomAtom atom;
        atom.nucleusAttraction  = UnityEngine.Random.Range(0f, _randomFactor * NUCLEUS_ATTR_MAX);
        atom.nucleusAttraction  = (UnityEngine.Random.Range(0, 2) > 0) ?
            Mathf.Max(atom.nucleusAttraction - previous.nucleusAttraction, 0f) :
            Mathf.Min(atom.nucleusAttraction + previous.nucleusAttraction, NUCLEUS_ATTR_MAX);

        atom.nucleusRepulsion   = UnityEngine.Random.Range(0f, _randomFactor * NUCLEUS_REPL_MAX);
        atom.nucleusRepulsion   = (UnityEngine.Random.Range(0, 2) > 0) ?
            Mathf.Max(atom.nucleusRepulsion - previous.nucleusRepulsion, 0f) :
            Mathf.Min(atom.nucleusRepulsion + previous.nucleusRepulsion, NUCLEUS_REPL_MAX);

        atom.nucleusSize        = UnityEngine.Random.Range(1f, _randomFactor * NUCLEUS_SIZE_MAX);
        atom.nucleusSize        = (UnityEngine.Random.Range(0, 2) > 0) ?
            Mathf.Max(atom.nucleusSize - previous.nucleusSize, 1f) :
            Mathf.Min(atom.nucleusSize + previous.nucleusSize, NUCLEUS_SIZE_MAX);

        atom.electronSize       = UnityEngine.Random.Range(1f, _randomFactor * ELECTR_SIZE_MAX);
        atom.electronSize       = (UnityEngine.Random.Range(0, 2) > 0) ?
            Mathf.Max(atom.electronSize - previous.electronSize, 1f) :
            Mathf.Min(atom.electronSize + previous.electronSize, ELECTR_SIZE_MAX);

        atom.electronSpeed      = UnityEngine.Random.Range(1f, _randomFactor * ELECTR_SPEED_MAX);
        atom.electronSpeed      = (UnityEngine.Random.Range(0, 2) > 0) ?
            Mathf.Max(atom.electronSpeed - previous.electronSpeed, 1f) :
            Mathf.Min(atom.electronSpeed + previous.electronSpeed, ELECTR_SPEED_MAX);

        atom.radialModifier = UnityEngine.Random.Range(1f, _randomFactor * RADIAL_MAX);
        atom.radialModifier = (UnityEngine.Random.Range(0, 2) > 0) ?
            Mathf.Max(atom.radialModifier - previous.radialModifier, 1f) :
            Mathf.Min(atom.radialModifier + previous.radialModifier, RADIAL_MAX);


        if (!StressTest)
        {
            atom.electronCount = UnityEngine.Random.Range(1f, _randomFactor * ELECTR_COUNT_MAX);
            atom.electronCount = (UnityEngine.Random.Range(0, 2) > 0) ?
                Mathf.Max(atom.electronCount - previous.electronCount, 1f) :
                Mathf.Min(atom.electronCount + previous.electronCount, ELECTR_COUNT_MAX);
        }
        else
        {
            atom.electronCount = Mathf.Min(previous.electronCount + STRESS_ADDITION, ELECTR_COUNT_STRESS);
        }

        return atom;
    }

}

#if UNITY_EDITOR

[CustomEditor(typeof(AtomRandomizer))]
public class AtomEditor : Editor
{
    private const float LABEL_WIDTH = 150F;

    private void CreateHeaderSpace(string header = " ")
    {
        EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
    }

    public override void OnInspectorGUI()
    {
        CreateHeaderSpace();

        var atomScript = target as AtomRandomizer;

        EditorGUIUtility.labelWidth = LABEL_WIDTH;

        atomScript.RandomizeBackground = EditorGUILayout.Toggle("Randomize Background", atomScript.RandomizeBackground);
        using (var randomBackground = new EditorGUILayout.FadeGroupScope(Convert.ToSingle(atomScript.RandomizeBackground)))
        {
            if (!randomBackground.visible)
            {
                EditorGUI.indentLevel++;
                atomScript.BackgroundColor = EditorGUILayout.ColorField(atomScript.BackgroundColor);
                EditorGUI.indentLevel--;
            }
        }

        CreateHeaderSpace();
        atomScript.RandomizeForeground = EditorGUILayout.Toggle("Randomize Foreground", atomScript.RandomizeForeground);
        using (var randomForeground = new EditorGUILayout.FadeGroupScope(Convert.ToSingle(atomScript.RandomizeForeground)))
        {
            if (!randomForeground.visible)
            {
                EditorGUI.indentLevel++;
                atomScript.ForegroundColor = EditorGUILayout.ColorField(atomScript.ForegroundColor);
                EditorGUI.indentLevel--;
            }
        }

        CreateHeaderSpace();
        CreateHeaderSpace("Atom Shader Fields");
        atomScript.RandomizeAtomFields = EditorGUILayout.Toggle("Randomize Atom Fields", atomScript.RandomizeAtomFields);

        using (var group = new EditorGUILayout.FadeGroupScope(Convert.ToSingle(atomScript.RandomizeAtomFields)))
        {
            if (group.visible)
            {
                EditorGUI.indentLevel++;
                atomScript.StressTest = EditorGUILayout.Toggle("Stress Test", atomScript.StressTest);

                using (var stressTest = new EditorGUI.DisabledScope(atomScript.StressTest))
                {
                    atomScript.RandomFactor = EditorGUILayout.Slider(atomScript.RandomFactor, 0.1f, 1.0f);
                }

                EditorGUI.indentLevel--;
            }
        }

        CreateHeaderSpace();
    }
}

#endif
