using UnityEngine;
using UnityEngine.Rendering;

// IMGUI funciona sin Canvas, TextMeshPro ni paquetes adicionales.
[DefaultExecutionOrder(-1000)]
public class Lab08EnemyPanel : MonoBehaviour
{
    static Lab08EnemyPanel instance;
    static bool suppressClick;
    public static bool IsPaused {get; private set;}
    public static bool BlocksWorldInput => IsPaused || suppressClick;
    [Range(.1f,2f)] public float markerRadius=.45f;
    public Color markerColor=new Color(.08f,.95f,.78f,1f);
    public float markerDuration=8f;
    Vector3 weights,initialWeights;
    bool capturedInitial;
    float previousTimeScale=1f;
    CursorLockMode previousCursorLock;
    bool previousCursorVisible;
    Transform player;
    LineRenderer ring;
    Material ringMaterial;
    Vector3 markerPoint;
    float markerUntil;
    GUIStyle heading,label,note,button;
    int enemyCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics(){instance=null;IsPaused=false;suppressClick=false;}
    void Awake()
    {
        if(instance!=null && instance!=this){enabled=false;return;}
        instance=this;
    }
    void Start()
    {
        var controller=FindAnyObjectByType<PlayerController>();
        if(controller!=null)player=controller.transform;
        ReadWeights();
        var go=new GameObject("MarcaDestino_Runtime");
        ring=go.AddComponent<LineRenderer>();ring.loop=true;ring.positionCount=64;
        ring.startWidth=.045f;ring.endWidth=.045f;ring.useWorldSpace=false;
        ring.shadowCastingMode=ShadowCastingMode.Off;ring.receiveShadows=false;
        Shader shader=Shader.Find("Unlit/Color");if(shader==null)shader=Shader.Find("Standard");
        ringMaterial=new Material(shader){color=markerColor};ring.sharedMaterial=ringMaterial;ring.enabled=false;
        for(int i=0;i<64;i++){
            float angle=i*Mathf.PI*2/64;
            ring.SetPosition(i,new Vector3(Mathf.Cos(angle)*markerRadius,0,Mathf.Sin(angle)*markerRadius));
        }
    }
    void ReadWeights()
    {
        var enemies=FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        enemyCount=enemies.Length;
        if(enemies.Length==0)return;
        weights=enemies[0].BoidWeights;
        if(!capturedInitial){initialWeights=weights;capturedInitial=true;}
    }
    void Apply()
    {
        var enemies=FindObjectsByType<EnemyController>(FindObjectsSortMode.None);enemyCount=enemies.Length;
        foreach(var enemy in enemies)enemy.SetBoidWeights(weights.x,weights.y,weights.z);
    }
    void Update()
    {
        if(instance!=this)return;
        if(suppressClick && !Input.GetMouseButton(0))suppressClick=false;
        if(Input.GetKeyDown(KeyCode.P)){
            if(IsPaused)Resume();else Pause();
        }
        if(ring==null)return;
        bool reached=player!=null && new Vector2(player.position.x-markerPoint.x,player.position.z-markerPoint.z).sqrMagnitude<.25f*.25f;
        if(reached || Time.time>markerUntil)ring.enabled=false;
        if(ring.enabled){
            float pulse=1f+.06f*Mathf.Sin(Time.time*5f);
            ring.transform.localScale=Vector3.one*pulse;
        }
    }
    public static void ShowDestination(Vector3 point)
    {
        if(instance==null || instance.ring==null || BlocksWorldInput)return;
        instance.markerPoint=point;
        instance.ring.transform.position=point+Vector3.up*.045f;
        instance.markerUntil=Time.time+instance.markerDuration;
        instance.ring.enabled=true;
    }
    void Pause()
    {
        ReadWeights();previousTimeScale=Time.timeScale;
        previousCursorLock=Cursor.lockState;previousCursorVisible=Cursor.visible;
        IsPaused=true;suppressClick=true;Time.timeScale=0f;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    void Resume()
    {
        if(!IsPaused)return;
        IsPaused=false;suppressClick=true;Time.timeScale=previousTimeScale;
        Cursor.lockState=previousCursorLock;Cursor.visible=previousCursorVisible;
    }
    void OnDisable(){if(instance==this){Resume();instance=null;}}
    void OnDestroy()
    {
        if(ring!=null)Destroy(ring.gameObject);
        if(ringMaterial!=null)Destroy(ringMaterial);
    }
    void Styles()
    {
        if(heading!=null)return;
        heading=new GUIStyle(GUI.skin.label){fontSize=23,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
        label=new GUIStyle(GUI.skin.label){fontSize=16};
        note=new GUIStyle(GUI.skin.label){fontSize=13,wordWrap=true};
        button=new GUIStyle(GUI.skin.button){fontSize=16};
    }
    float Slider(string text,float value)
    {
        GUILayout.Label(text+"    "+value.ToString("0.00"),label);
        float next=GUILayout.HorizontalSlider(value,0f,10f,GUILayout.Height(22));
        return Mathf.Round(next*100f)/100f;
    }
    void OnGUI()
    {
        if(instance!=this)return;Styles();
        if(!IsPaused){GUI.Box(new Rect(12,12,265,32),"P: pausa y parametros de flocking");return;}
        // Escalado para que el panel quepa en ventanas Game pequenas.
        float scale=Mathf.Min(1f,Screen.width/560f,Screen.height/570f);
        Matrix4x4 previous=GUI.matrix;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        float width=480,height=510;
        Rect rect=new Rect((Screen.width/scale-width)/2,(Screen.height/scale-height)/2,width,height);
        GUI.Box(rect,GUIContent.none);
        GUILayout.BeginArea(new Rect(rect.x+24,rect.y+16,width-48,height-30));
        GUILayout.Label("PAUSA - FLOCKING",heading);
        GUILayout.Label("Ajusta los pesos de todos los enemigos.",note);
        GUILayout.Space(10);
        Vector3 old=weights;
        weights.x=Slider("Separacion",weights.x);
        weights.y=Slider("Alineamiento",weights.y);
        weights.z=Slider("Cohesion",weights.z);
        if(weights!=old)Apply();
        GUILayout.Space(10);
        GUILayout.Label("Enemigos actualizados: "+enemyCount,note);
        GUILayout.Label("Los valores se aplican al mover las barras. Pulsa P para ver el movimiento con el nuevo ajuste.",note);
        GUILayout.Space(12);
        if(GUILayout.Button("Restablecer valores iniciales",button,GUILayout.Height(34))){weights=initialWeights;Apply();}
        GUILayout.Space(8);
        if(GUILayout.Button("Continuar  [P]",button,GUILayout.Height(38)))Resume();
        GUILayout.EndArea();GUI.matrix=previous;
    }
}
