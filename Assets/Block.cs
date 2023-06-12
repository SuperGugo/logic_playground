using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class Block : MonoBehaviour
{
    public bool[] sides = new bool[4];
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
    public Sprite lightoff;
    public Sprite lighton;
    public Sprite switchoff;
    public Sprite switchon;
    public int[] inputs;
    public int[] outputs;
    public Dictionary<Block,int> connectables = new Dictionary<Block, int>();
    private SpriteRenderer rend;
    public int blocknum;
    public SimulationController simcontrol;
    // Start is called before the first frame update
    void Start() {
      rend = gameObject.GetComponent<SpriteRenderer>();

    }
    public void StartSimulation() {
      connectables = new Dictionary<Block, int>();
      GameObject[] allblocks = GameObject.FindGameObjectsWithTag("Block");
      foreach (GameObject block in allblocks) {
        if (Vector2.Distance(block.transform.position, transform.position) == 1) {
          Vector2 dist = block.transform.position - transform.position;
          Block blockcontroller = block.GetComponent<Block>();
          foreach (int input in blockcontroller.inputs) {
            foreach (int output in outputs) {
              if (input - 2 == output) {
                if (output == 0 && dist == new Vector2(0, 1)) {
                  connectables.Add(blockcontroller, input);
                } else if (output == 1 && dist == new Vector2(1, 0)) {
                  connectables.Add(blockcontroller, input);
                } else if (output == 2 && dist == new Vector2(0, -1)) {
                  connectables.Add(blockcontroller, input);
                } else if (output == 3 && dist == new Vector2(-1, 0)) {
                  connectables.Add(blockcontroller, input);
                }
              } else if (output - 2 == input) {
                if (output == 0 && dist == new Vector2(0, 1)) {
                  connectables.Add(blockcontroller, input);
                } else if (output == 1 && dist == new Vector2(1, 0)) {
                  connectables.Add(blockcontroller, input);
                } else if (output == 2 && dist == new Vector2(0, -1)) {
                  connectables.Add(blockcontroller, input);
                } else if (output == 3 && dist == new Vector2(-1, 0)) {
                  connectables.Add(blockcontroller, input);
                }
              }
            }
          }

        }
      }

    }

    void OnMouseOver()
    {
      if(Input.GetMouseButtonDown(0)){
        if (button) {
          if (sides[0]) {
           rend.sprite = switchoff;
           sides[0] = false;
           sides[1] = false;
           sides[2] = false;
           sides[3] = false;
         } else {
           rend.sprite = switchon;
           sides[0] = true;
           sides[1] = true;
           sides[2] = true;
           sides[3] = true;
         }
       }
     } else if(Input.GetKeyDown(KeyCode.C) | Input.GetMouseButtonDown(2)){
          simcontrol.currentBlock = blocknum;
       }else if(Input.GetKeyDown(KeyCode.R)){
           if (wirecurve) {
             if (rend.flipX) {
               transform.eulerAngles = new Vector3(0f, 0f, transform.eulerAngles.z+90f);
             } else {
               transform.eulerAngles = new Vector3(0f, 0f, transform.eulerAngles.z-90f);
             }
             rend.flipX = !rend.flipX;
             int in2 = inputs[0];
             int out2 = outputs[0];
             outputs[0] = in2;
             inputs[0]  = out2;
           } else if (bridgeWire) {
             rend.flipY = !rend.flipY;
             int in2 = inputs[0];
             int out2 = outputs[0];
             outputs[0] = in2;
             inputs[0]  = out2;
           } else if (dLatch) {
             rend.flipX = !rend.flipX;
             int in2 = inputs[1];
             int out2 = outputs[1];
             outputs[1] = in2;
             inputs[1]  = out2;
           }
       }else if(Input.GetKeyDown(KeyCode.Q)){
          transform.eulerAngles = new Vector3(0f, 0f, transform.eulerAngles.z+90);
          foreach (int input in inputs) {
            int in2 = Array.IndexOf(inputs,input);
            inputs[in2]--;
            if(inputs[in2] == -1) {
              inputs[in2]=3;
            }
          }
          foreach (int output in outputs) {
            int out2 = Array.IndexOf(outputs,output);
            outputs[out2]--;
            if(outputs[out2] == -1) {
              outputs[out2]=3;
            }
          }
      }else if(Input.GetKeyDown(KeyCode.E)){
        transform.eulerAngles = new Vector3(0f, 0f, transform.eulerAngles.z-90f);
        foreach (int input in inputs) {
          int in2 = Array.IndexOf(inputs,input);
          inputs[in2]++;
          if(inputs[in2] == 4) {
            inputs[in2]=0;
          }
        }
        foreach (int output in outputs) {
          int out2 = Array.IndexOf(outputs,output);
          outputs[out2]++;
          if(outputs[out2] == 4) {
            outputs[out2]=0;
          }
        }
      }

    }

    // Update is called once per frame
    void Update()
    {

      if (wire) {
        sides[outputs[0]] = sides[inputs[0]];

      }
      if (tWire) {
        sides[outputs[0]] = sides[inputs[0]];
        sides[outputs[1]] = sides[inputs[0]];
      }
      if (crossWire) {
        sides[outputs[0]] = sides[inputs[0]];
        sides[outputs[1]] = sides[inputs[0]];
        sides[outputs[2]] = sides[inputs[0]];
      }
      if (bridgeWire) {
        sides[outputs[0]] = sides[inputs[0]];
        sides[outputs[1]] = sides[inputs[1]];

      }
      if (not) {
        sides[outputs[0]] = !sides[inputs[0]];
      }
      if (or) {
        sides[outputs[0]] = negate^(sides[inputs[0]] | sides[inputs[1]]);
      }
      if (and) {
        sides[outputs[0]] = negate^(sides[inputs[0]] && sides[inputs[1]]);
      }
      if (xor) {
        sides[outputs[0]] = negate^(sides[inputs[0]] ^ sides[inputs[1]]);
      }
      if (multiplexer) {
        sides[outputs[0]] = sides[inputs[(sides[inputs[0]])?2:1]];
      }
      if (dLatch) {
        sides[outputs[1]] = sides[inputs[1]];
        if (sides[inputs[1]]) {
            sides[outputs[0]] = sides[inputs[0]];
        }
      }
      if (demultiplexer) {
        sides[outputs[0]] = sides[inputs[0]]&!sides[inputs[1]];
        sides[outputs[1]] = sides[inputs[0]]&sides[inputs[1]];
      }
      if (light) {
        if (sides[0] | sides[1] | sides[2] | sides[3]) {
          rend.sprite = lighton;
        } else {
          rend.sprite = lightoff;
        }
      }

      if (connectables != null) {
        foreach(KeyValuePair<Block, int> connectable in connectables) {
          int outp = 0;
          int inp = connectable.Value;
          if (inp > 1) {
            outp = inp-2;
          } else {
            outp = inp+2;
          }
          connectable.Key.sides[inp] = sides[outp];
        }
      }
    }

}
