using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using TMPro;
using System.Linq;

public class SimulationController : MonoBehaviour
{
    public TextMeshProUGUI tms;
    public TextMeshProUGUI code;
    public TMP_InputField insaveload;
    public GameObject[] blocks;
    public UnityEngine.UI.Image thumbnail;
    public int currentBlock;
    public Camera cam;
    public bool keyboardmode = true;
    public List<string> savefile;
    public Sprite selectionRenderer;
    public Sprite deleteRenderer;
    public float blinkspeed = 2f;
    public GameObject listitemprefab;
    public float scrollspeed;
    public Transform listcontainer;
    public List<Sprite> sprites;
    public UnityEngine.UI.Image deletebutton;
    Vector2 startangle = Vector2.zero;
    Vector2 endangle = Vector2.zero;
    bool copying = false;
    bool rendersel = false;
    bool renderdel = false;
    SpriteRenderer rect;
    List<GameObject> renderblocks;
    Vector2 oldworldpos;
    float lastwidth;
    bool menu = false;
    bool deletingelement = false;


    public void startDeletingElement() {
      deletingelement = true;
      deletebutton.color = new Color(1, 0.2666666667f, 0.2666666667f,0.6862745098f);
    }

    // start the simulation
    public void StartSim()
    {
      GameObject[] allblocks = GameObject.FindGameObjectsWithTag("Block");
      foreach (GameObject block in allblocks) {
        Block blockcontroller = block.GetComponent<Block>();
        blockcontroller.StartSimulation();
      }
    }

    // serialize and save the state of the game
    public void save()
    {
        GameObject[] allblocks = GameObject.FindGameObjectsWithTag("Block");
        savefile = new List<string>();
        foreach (GameObject obj in allblocks) {
            block serializable = new block(obj,sprites);
            savefile.Add(JsonUtility.ToJson(serializable));
        }
        PlayerPrefs.SetString(insaveload.text, string.Join("&&", savefile));
        List<string> list = PlayerPrefs.GetString("saveslist").Split("||").ToList();
        if (!list.Contains(insaveload.text)) {
          list.Add(insaveload.text);
          GameObject button = Instantiate(listitemprefab, listcontainer);
          button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => load(insaveload.text));
          button.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = insaveload.text;
        }
        PlayerPrefs.SetString("saveslist", string.Join("||", list));
    }

    // load the game from playerprefs
    public void load(string savename) {
        if (deletingelement) {
          deletebutton.color = new Color(0.3215686275f, 0.3215686275f, 0.3215686275f, 0.5176470588f);
          List<string> list = PlayerPrefs.GetString("saveslist").Split("||").ToList();
          list.Remove(savename);
          PlayerPrefs.SetString("saveslist", string.Join("||", list));
          foreach (Transform child in listcontainer) {
            if (child.GetChild(0).GetComponent<TextMeshProUGUI>().text == savename) {
              Destroy(child.gameObject);
              break;
            }
          }
          deletingelement = false;
        } else {
          GameObject[] allblocks = GameObject.FindGameObjectsWithTag("Block");
          foreach (GameObject block in allblocks) {
            Destroy(block);
          }
          string jsonString = PlayerPrefs.GetString(savename);
          print(jsonString);
          string[] savefile = jsonString.Split("&&");
          foreach (string obj in savefile) {
              block deserialized = JsonUtility.FromJson<block>(obj);
              print(deserialized);
              deserialized.blockToGO(sprites);
          }
          menu = false;
          listcontainer.parent.parent.gameObject.SetActive(false);
        }
    }

    // checks if a point is between two other points
    bool between(Vector2 point, Vector2 topleft, Vector2 bottomright) {
      return point.x >= topleft.x && point.y <= topleft.y && point.x <= bottomright.x && point.y >= bottomright.y;
    }

    // copy and paste a part of the map
    List<GameObject> paste(Vector2 pasteangle, Vector2 topleft, Vector2 bottomright, Vector2 placepoint, bool renderonly = false) {
        List<GameObject> allblocks = new List<GameObject>(GameObject.FindGameObjectsWithTag("Block"));
        List<GameObject> newlist = new List<GameObject>();
        foreach (GameObject block in allblocks.Where(block => between(block.transform.position, topleft, bottomright))) {
          GameObject newblock;
          if (!allblocks.Any(b => b.transform.position == block.transform.position+(Vector3)(pasteangle-placepoint))) {
            newblock = Instantiate(block);
            newblock.transform.position += (Vector3)(pasteangle-placepoint);
            if (renderonly) {
              newblock.tag = "TempRender";
              Destroy(newblock.GetComponent<BoxCollider2D>());
              Destroy(newblock.GetComponent<Block>());
              // newblock.GetComponent<SpriteRenderer>().color = new Color(1,1,1,(Mathf.Sin(Time.timeSinceLevelLoad*blinkspeed)+1)/2*0.75f+0.25f);
            }
            newlist.Add(newblock);
          }
        }
        return(newlist);
    }

    // place a block, as easy as that
    void place(GameObject blockToPlace, Vector2 position) {
      List<GameObject> allblocks = new List<GameObject>(GameObject.FindGameObjectsWithTag("Block"));
      bool canPlace = allblocks.Any(block => block.transform.position == (Vector3)position);
      if (!canPlace) {
        GameObject instantiatedBlock =Instantiate(blockToPlace, position, Quaternion.identity);
        instantiatedBlock.GetComponent<Block>().blocknum = currentBlock;
        instantiatedBlock.GetComponent<Block>().simcontrol = GetComponent<SimulationController>();
      }

    }

    // renders a rectangle in that position with that scale with that sprite
    void renderRect(Vector2 pos, float scalex, float scaley, Sprite sprite) {
      rect = new GameObject().AddComponent<SpriteRenderer>();
      rect.transform.position = pos;
      rect.sprite = sprite;
      rect.drawMode = SpriteDrawMode.Tiled;
      rect.sortingOrder = 1;
      rect.size = new Vector2(scalex+Mathf.Sign(scalex),scaley+Mathf.Sign(scaley));
    }

    void Start() {
      List<string> saveslist = PlayerPrefs.GetString("saveslist").Split("||").ToList();
      foreach (string name in saveslist) {
        if (name != "") {
          GameObject button = Instantiate(listitemprefab, listcontainer);
          button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => load(name));
          button.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = name;
        }
      }
    }

    // this mf runs each tick
    void Update()
      {
        float width = listcontainer.parent.GetComponent<RectTransform>().rect.width;
        if (lastwidth != width) {
          listcontainer.GetComponent<GridLayoutGroup>().cellSize = new Vector3(width, 100);
          lastwidth = width;
        }

        Vector3 mousePos = Input.mousePosition;
        mousePos.z=Camera.main.nearClipPlane;
        Vector3 Worldpos=Camera.main.ScreenToWorldPoint(mousePos);
        Vector2 Worldpos2D=new Vector2(Mathf.Round(Worldpos.x),Mathf.Round(Worldpos.y));

        if (Input.GetKeyDown(KeyCode.Escape)) {
          menu = !menu;
          listcontainer.parent.parent.gameObject.SetActive(menu);
        }

        if (!menu) {
          if (Input.GetMouseButtonDown(1)) {
            if (renderblocks != null) {
              foreach (GameObject block in renderblocks) {
                Destroy(block);
              }
            }
            copying = false;
            rendersel = false;
            startangle = Worldpos2D;
            renderdel = true;
          } else if (Input.GetKeyUp(KeyCode.F) && rendersel) {
            endangle = Worldpos2D;
            rendersel = false;
            copying = true;
          } else if (Input.GetKeyDown(KeyCode.F) && !renderdel) {
            if (renderblocks != null) {
              foreach (GameObject block in renderblocks) {
                Destroy(block);
              }
            }
            renderblocks = new List<GameObject>();
            startangle = Worldpos2D;
            rendersel = true;
            copying = false;
          }

          if (Input.GetKeyDown(KeyCode.D)) {
            if (currentBlock < blocks.Length-1) {
              currentBlock++;
            } else {
              currentBlock = 0;
            }

          } else if (Input.GetKeyDown(KeyCode.A)) {
            if (currentBlock > 0) {
              currentBlock--;
            } else {
              currentBlock = blocks.Length-1;
            }

          }

          if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            cam.transform.position-=new Vector3(1f,0f,0f);
          } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
            cam.transform.position+=new Vector3(1f,0f,0f);
          }

          if (Input.GetKeyDown(KeyCode.UpArrow)) {
            cam.transform.position+=new Vector3(0f,1f,0f);
          } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
            cam.transform.position-=new Vector3(0f,1f,0f);
          }

          if (Input.GetAxis("Mouse ScrollWheel") > 0f) {
            if (cam.orthographicSize > 0.5) {
              cam.orthographicSize/=2;
              Vector2 newpos = (((Vector2)cam.transform.position + Worldpos2D) / 2);
              cam.transform.position = new Vector3(newpos.x, newpos.y, -10);
            }
          } else if (Input.GetAxis("Mouse ScrollWheel") < 0f) {
            if (cam.orthographicSize < 32) {
              cam.orthographicSize*=2;
              Vector2 newpos = (Vector2)cam.transform.position-(Worldpos2D-(Vector2)cam.transform.position);
              cam.transform.position = new Vector3(newpos.x, newpos.y, -10);
            }
          }

          if (Input.GetKeyDown(KeyCode.S) | Input.GetMouseButton(0)) {
            if (copying) {
              copying = false;
              Vector2 minangle = new Vector3(Mathf.Min(startangle.x, endangle.x),Mathf.Max(startangle.y, endangle.y));
              Vector2 maxangle = new Vector3(Mathf.Max(startangle.x, endangle.x),Mathf.Min(startangle.y, endangle.y));
              foreach (GameObject block in renderblocks) {
                Destroy(block);
              }
              paste(Worldpos2D, minangle, maxangle, startangle);
            } else {
              place(blocks[currentBlock], Worldpos2D);
            }
          }
          if (Input.GetMouseButtonUp(1)) {
            renderdel = false;
            Destroy(rect.gameObject);
            GameObject[] allblocks = GameObject.FindGameObjectsWithTag("Block");
            Vector2 minangle = new Vector3(Mathf.Min(startangle.x, Worldpos2D.x),Mathf.Max(startangle.y, Worldpos2D.y));
            Vector2 maxangle = new Vector3(Mathf.Max(startangle.x, Worldpos2D.x),Mathf.Min(startangle.y, Worldpos2D.y));
            foreach (GameObject block in allblocks) {
                if (between(block.transform.position, minangle, maxangle)) {
                  Destroy(block);
                }
            }
          }
        }

        if (rendersel || renderdel) {
          float scalex = startangle.x - Worldpos2D.x;
          float scaley = startangle.y - Worldpos2D.y;
          Vector3 pos = new Vector3(startangle.x-(scalex)/2,startangle.y-(scaley)/2, 0);
          Sprite sprite = rendersel ? selectionRenderer : deleteRenderer;
          if (rect == null) {
            renderRect(pos,scalex,scaley,sprite);
          } else {
            if (rect.transform.position != pos || rect.sprite != sprite) {
              Destroy(rect.gameObject);
              renderRect(pos,scalex,scaley,sprite);
            }
          }
        }

        if (copying) {
          if (rect != null) {
            Destroy(rect.gameObject);
          }

          /*
          if (renderblocks != null) {
            foreach (GameObject block in renderblocks) {
              block.GetComponent<SpriteRenderer>().color = new Color(1,1,1,(Mathf.Sin(Time.timeSinceLevelLoad*blinkspeed)+1)/2*0.75f+0.25f);
            }
          }
          */

          if (oldworldpos != Worldpos2D) {
            Vector2 minangle = new Vector3(Mathf.Min(startangle.x, endangle.x),Mathf.Max(startangle.y, endangle.y));
            Vector2 maxangle = new Vector3(Mathf.Max(startangle.x, endangle.x),Mathf.Min(startangle.y, endangle.y));
            if (renderblocks != null) {
              foreach (GameObject block in renderblocks) {
                Destroy(block);
              }
            }
            renderblocks = paste(Worldpos2D, minangle, maxangle, startangle, true);;
            oldworldpos = Worldpos2D;
          }
        }

        tms.text = blocks[currentBlock].name;
        thumbnail.sprite = blocks[currentBlock].GetComponent<SpriteRenderer>().sprite;

  }



}

public class block
{
    public string name;
    public Vector3 position;
    public Quaternion rotation;
    public int sprite;
    public bool flipx;
    public bool flipy;
    public bool[] sides;
    public bool not;
    public bool wire;
    public bool tWire;
    public bool crossWire;
    public bool bridgeWire;
    public bool light;
    public bool button;
    public bool or;
    public bool and;
    public bool xor;
    public bool multiplexer;
    public bool demultiplexer;
    public bool negate;
    public bool dLatch;
    public bool wirecurve;
    public int lightoff;
    public int lighton;
    public int switchoff;
    public int switchon;
    public int[] inputs;
    public int[] outputs;
    public Dictionary<Block,int> connectables = new Dictionary<Block,int>();
    public int blocknum;
    public SimulationController simcontrol;
    public block(GameObject obj, List<Sprite> spritelist) {
        SpriteRenderer rend = obj.GetComponent<SpriteRenderer>();
        Block b = obj.GetComponent<Block>();
        this.name = obj.name;
        this.position = obj.transform.position;
        this.rotation = obj.transform.rotation;
        this.sprite = spritelist.IndexOf(rend.sprite);
        this.flipx = rend.flipX;
        this.flipy = rend.flipY;
        this.sides = b.sides;
        this.not = b.not;
        this.wire = b.wire;
        this.tWire = b.tWire;
        this.crossWire = b.crossWire;
        this.bridgeWire = b.bridgeWire;
        this.light = b.light;
        this.button = b.button;
        this.or = b.or;
        this.and = b.and;
        this.xor = b.xor;
        this.multiplexer = b.multiplexer;
        this.demultiplexer = b.demultiplexer;
        this.negate = b.negate;
        this.dLatch = b.dLatch;
        this.wirecurve = b.wirecurve;
        this.lightoff = 11;
        this.lighton = 16;
        this.switchoff = 9;
        this.switchon = 10;
        this.inputs = b.inputs;
        this.outputs = b.outputs;
        this.connectables = b.connectables;
        this.blocknum = b.blocknum;
        this.simcontrol = b.simcontrol;
    }
    public GameObject blockToGO(List<Sprite> spritelist)
        {
            GameObject obj = new GameObject(name);
            SpriteRenderer rend = obj.AddComponent<SpriteRenderer>();
            Block b = obj.AddComponent<Block>();
            obj.AddComponent<BoxCollider2D>().size = new Vector2(1,1);
            obj.tag = "Block";
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            rend.sprite = spritelist[sprite];
            rend.flipX = flipx;
            rend.flipY = flipy;
            b.sides = sides;
            b.not = not;
            b.wire = wire;
            b.tWire = tWire;
            b.crossWire = crossWire;
            b.bridgeWire = bridgeWire;
            b.light = light;
            b.button = button;
            b.or = or;
            b.and = and;
            b.xor = xor;
            b.multiplexer = multiplexer;
            b.demultiplexer = demultiplexer;
            b.negate = negate;
            b.dLatch = dLatch;
            b.wirecurve = wirecurve;
            b.lightoff = spritelist[lightoff];
            b.lighton = spritelist[lighton];
            b.switchoff = spritelist[switchoff];
            b.switchon = spritelist[switchon];
            b.inputs = inputs;
            b.outputs = outputs;
            b.connectables = connectables;
            b.blocknum = blocknum;
            b.simcontrol = simcontrol;
            return obj;
        }
}
