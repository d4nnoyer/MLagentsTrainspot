using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using Unity.Barracuda;
using Unity.Barracuda.ONNX;
using System.IO;
using System;


namespace UnityPipes
{

    public class MoveToGoalAgent : Agent
    {
        [SerializeField] private Transform TargetTransform;
        [SerializeField] private Material winMaterial;
        [SerializeField] private Material loseMaterial;
        [SerializeField] private MeshRenderer floorMeshRenderer;

        public bool isRandomnessEnabled = true;
        Agent ag;

        private void Start()
        {
            ag = GetComponent<Agent>();
            ag.LazyInitialize();
        }

        public override void OnEpisodeBegin()
        {
            if (isRandomnessEnabled)
            {
                transform.localPosition = new Vector3(UnityEngine.Random.Range(-2f, 2f), 0, UnityEngine.Random.Range(-0.5f, 2f));
                TargetTransform.localPosition = new Vector3(UnityEngine.Random.Range(3.5f, 7f), 0, UnityEngine.Random.Range(-2f, 1f));
            }
            else
            {
                transform.localPosition = Vector3.zero;
                TargetTransform.localPosition = new Vector3(2, 0, 0);
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.localPosition);
            sensor.AddObservation(TargetTransform.localPosition);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveX = actions.ContinuousActions[0];
            float moveZ = actions.ContinuousActions[1];

            float moveSpeed = 5f;
            transform.localPosition += new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed;
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
            continuousActions[0] = Input.GetAxisRaw("Horizontal");
            continuousActions[1] = Input.GetAxisRaw("Vertical");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<Goal>(out _))
            {
                SetReward(+1f);
                floorMeshRenderer.material = winMaterial;
                EndEpisode();
            }

            if (other.TryGetComponent<Wall>(out _))
            {
                SetReward(-1f);
                floorMeshRenderer.material = loseMaterial;
                EndEpisode();
            }
        }

        public void OverrideOnnxModel(string assetPath)
        {
            byte[] rawModel = null;
            try
            {
                rawModel = File.ReadAllBytes(assetPath);
            }
            catch (IOException)
            {
                Debug.Log($"Couldn't load file {assetPath}", this);
            }

            var converter = new ONNXModelConverter(true);
            NNModelData assetData = ScriptableObject.CreateInstance<NNModelData>();

            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                ModelWriter.Save(writer, converter.Convert(rawModel));
                assetData.Value = memoryStream.ToArray();
            }

            assetData.name = "Data";
            assetData.hideFlags = HideFlags.HideInHierarchy;

            var asset = ScriptableObject.CreateInstance<NNModel>();
            asset.modelData = assetData;

            asset.name = "External" + Path.GetFileName(assetPath);

            if (asset != null)
            {
                try
                {
                    ag.SetModel(GetComponent<BehaviorParameters>().BehaviorName, asset);
                }
                catch (Exception)
                {
                }
            }

        }
    }
}