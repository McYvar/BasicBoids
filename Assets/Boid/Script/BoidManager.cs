using UnityEngine;

public class BoidManager : MonoBehaviour
{
    [SerializeField] private GameObject boidPrefab;
    [SerializeField] private int boidAmount;
    [SerializeField, Range(1f, 50f)] private float maxSpeed;
    [SerializeField, Range(0.01f, 5f)] private float maxRangeToOtherBoids;
    [SerializeField, Range(0.01f, 1f)] private float matchVelocityAmplifier;

    [SerializeField] private bool useBorder;

    [Space(20), SerializeField] private Vector3 boundariesCentre;
    [SerializeField] private float boundariesSize;

    private float nextPositionReductor = 100;
    private Transform[] boidTransforms;
    private Vector3[] boidVelocities;

    private void Start()
    {
        boidTransforms = new Transform[boidAmount];
        boidVelocities = new Vector3[boidAmount];

        for (int i = 0; i < boidAmount; i++)
        {
            boidTransforms[i] = Instantiate(boidPrefab, GetRandomLocationWithinRange(boundariesCentre, boundariesSize), Quaternion.identity).transform;
        }
    }

    private void Update()
    {
        Vector3 centreOfMass = FindCOM(boidTransforms);
        Vector3 groupVelo = Vector3.zero;
        for (int i = 0; i < boidAmount; i++)
        {
            Vector3 v1 = ExcludeSelfFromCOM(centreOfMass, boidTransforms[i].position, boidAmount) / nextPositionReductor;
            Vector3 v2 = DisplacementIfCloseOrOutOfRange(boidTransforms, boidTransforms[i].position, maxRangeToOtherBoids, boundariesCentre, boundariesSize);
            Vector3 v3 = MatchVelocity(boidVelocities, boidVelocities[i], matchVelocityAmplifier);

            boidVelocities[i] += v1 + v2 + v3;
            boidVelocities[i] = LimitSpeed(boidVelocities[i], maxSpeed);

            Vector3 nextPos = boidTransforms[i].position + boidVelocities[i] * Time.deltaTime;
            boidTransforms[i].LookAt(nextPos);
            boidTransforms[i].position = nextPos;

            groupVelo += boidVelocities[i];
        }
        Debug.DrawLine(centreOfMass / boidAmount, groupVelo);
    }

    #region Rule1
    // Method is excluding dividing by the array length --> I do this at a later stage
    private Vector3 FindCOM(Transform[] entities) //COM == CentreOfMass
    {
        Vector3 centre = Vector3.zero;
        foreach (var entity in entities)
        {
            centre += entity.position;
        }
        return centre;
    }

    // Here we include dividing by the array length, update function doesn't have to loop FindCOM for each boid now :)
    private Vector3 ExcludeSelfFromCOM(Vector3 com, Vector3 myPosition, int arrayLength)
    {
        return (com - myPosition) / (arrayLength - 1) - myPosition;
    }
    #endregion

    #region Rule2
    private Vector3 DisplacementIfCloseOrOutOfRange(Transform[] entities, Vector3 myPosition, float maxRangeToOtherEntities, Vector3 origin, float maxRange)
    {
        // If this boid is outside the edge of the boundary, instandly change the velocity to inwards again
        if (useBorder)
        {
            float rangeToOrigin = Vector3.Distance(myPosition, origin);
            if (rangeToOrigin >= maxRange)
            {
                float rangeOutsideMaxRange = rangeToOrigin - maxRange;
                return (origin - myPosition).normalized * rangeOutsideMaxRange;
            }
        }

        Vector3 displacement = Vector3.zero;
        foreach (var entity in entities)
        {
            if (entity.position != myPosition)
            {
                // If this boid is close to another boid rule
                if (Vector3.Distance(myPosition, entity.position) < maxRangeToOtherEntities)
                {
                    displacement += myPosition - entity.position;
                }
            }
        }

        return displacement;
    }
    #endregion

    #region Rule3
    private Vector3 MatchVelocity(Vector3[] entitiesVelocity, Vector3 myVelocity, float amplifier)
    {
        Vector3 velocity = Vector3.zero;
        foreach (var entityVel in entitiesVelocity)
        {
            if (entityVel != myVelocity)
            {
                velocity += entityVel;
            }
        }
        velocity /= entitiesVelocity.Length - 1;
        velocity *= amplifier;
        return velocity;
    }
    #endregion

    private Vector3 LimitSpeed(Vector3 myVelocity, float maxSpeed)
    {
        if (myVelocity.magnitude > maxSpeed)
        {
            myVelocity = myVelocity.normalized * maxSpeed;
        }
        return myVelocity;
    }

    private Vector3 GetRandomLocationWithinRange(Vector3 origin, float range)
    {
        return origin + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * range * Random.value;
    }

    private void OnDrawGizmos()
    {
        if (!useBorder) return;
        Gizmos.color = new Color(0, 1, 0.3f, 0.1f);
        Gizmos.DrawSphere(boundariesCentre, boundariesSize);
    }
}
