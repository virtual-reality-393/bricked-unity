using UnityEngine;
using Unity.Sentis;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Meta.XR;
using Meta.XR.EnvironmentDepth;
using PassthroughCameraSamples;
using FF = Unity.Sentis.Functional;

public class ModelQuantization : MonoBehaviour
{
    public ModelAsset modelAsset;
    void Start()
    {
        QuantizeModel();
    }

    void QuantizeModel()
    {
        //Load model
        var model = ModelLoader.Load(modelAsset);

        //Here we transform the output of the model by feeding it through a Non-Max-Suppression layer.
        var graph = new FunctionalGraph();
        var input = graph.AddInput(model, 0);

        var centersToCornersData = new[]
        {
                        1,      0,      1,      0,
                        0,      1,      0,      1,
                        -0.5f,  0,      0.5f,   0,
                        0,      -0.5f,  0,      0.5f
            };
        var centersToCorners = FF.Constant(new TensorShape(4, 4), centersToCornersData);
        var modelOutput = FF.Forward(model, input)[0];  //shape(1,N,85)
        //                                                // Following for yolo model. in (1, 84, N) out put shape
        //var boxCoords = modelOutput[0, ..4, ..].Transpose(0, 1);
        //var allScores = modelOutput[0, 4.., ..].Transpose(0, 1);
        //var scores = FF.ReduceMax(allScores, 1);    //shape=(N)
        //var classIDs = FF.ArgMax(allScores, 1); //shape=(N)
        //var boxCorners = FF.MatMul(boxCoords, centersToCorners);    //shape=(N,4)
        //var indices = FF.NMS(boxCorners, scores, 0.4f, 0.4f); //shape=(N)
        //var indices2 = indices.Unsqueeze(-1).BroadcastTo(new[] { 4 });  //shape=(N,4)
        //var labelIDs = FF.Gather(classIDs, 0, indices); //shape=(N)
        //var coords = FF.Gather(boxCoords, 0, indices2); //shape=(N,4)

        var modelFinal = graph.Compile(modelOutput);

        //Export the model to Sentis format
        ModelQuantizer.QuantizeWeights(QuantizationType.Uint8, ref modelFinal);
        ModelWriter.Save("model.sentis", modelFinal);
    }
}
