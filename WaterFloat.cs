using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterFloatHelper
{
    public Transform t = null;
    public Rigidbody rb = null;
    public bool set_drag = false;
    public bool add_force = false;
    public float transition_time = 0f;

    public float original_drag = 0f;
    public float original_angular_drag = 0f;

    public WaterFloatHelper(Transform _t, Rigidbody _rb, float od, float oad)
    {
        rb = _rb;
        t = _t;
        set_drag = false;
        add_force = false;
        transition_time = 0f;
        original_drag = od;
        original_angular_drag = oad;
    }
}

public class WaterFloat : MonoBehaviour
{
    public float force = 0.1f;

    public float waterDrag = 1;

    private float water_height = 0f;

    private HashSet<Transform> transform_set = new HashSet<Transform>();
    private List<WaterFloatHelper> item_list = new List<WaterFloatHelper>();
    private Dictionary<Transform, int> transform_to_int = new Dictionary<Transform, int>();

    public void Start()
    {
        water_height = transform.position.y;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!other.attachedRigidbody)
        {
            return;
        }
        if (!transform_set.Contains(other.transform))
        {
            transform_set.Add(other.transform);
            item_list.Add(new WaterFloatHelper(other.transform, other.attachedRigidbody, other.attachedRigidbody.drag, other.attachedRigidbody.angularDrag));
            transform_to_int[other.transform] = item_list.Count - 1;
        }
    }

    public void OnTriggerStay(Collider other)
    {
        if (transform_set.Contains(other.transform))
        {
            int loc = transform_to_int[other.transform];

            addForce(loc);

            if (item_list[loc].add_force)
            {
                item_list[loc].rb.useGravity = true;
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (transform_set.Contains(other.transform))
        {
            int loc = transform_to_int[other.transform];

            item_list[loc].rb.useGravity = true;
            item_list[loc].rb.drag = item_list[loc].original_drag;
            item_list[loc].rb.angularDrag = item_list[loc].original_angular_drag;

            item_list.RemoveAt(loc);

            transform_to_int.Remove(other.transform);

            transform_set.Remove(other.transform);

            List<Transform> to_adjust = new List<Transform>();

            foreach (KeyValuePair<Transform, int> entry in transform_to_int)
            {
                if(entry.Value > loc)
                {
                    to_adjust.Add(entry.Key);
                }
            }

            for(int i = 0; i < to_adjust.Count; i++)
            {
                transform_to_int[to_adjust[i]] = transform_to_int[to_adjust[i]] - 1;
            }
        }
    }

    public float max_weight = 20f;
    public float mass_force_mult = 2f;
    public Vector3 down_stream_float_strength;
    public void addForce(int loc)
    {
        Transform obj = item_list[loc].t;
        var distance2 = Vector3.Distance(transform.position, obj.position);

        item_list[loc].add_force = true;

        if (item_list[loc].t.position.y < water_height)
        {
            item_list[loc].rb.useGravity = false;
            float rough_mass_calc = item_list[loc].rb.mass < 1f ? 1f : 1.0f+(Mathf.InverseLerp(1f, max_weight, item_list[loc].rb.mass)) * mass_force_mult;
            item_list[loc].rb.AddForce(Vector3.up * (force+Mathf.Abs(Physics.gravity.y)) * rough_mass_calc);
        }
        else if (item_list[loc].t.position.y >= water_height)
        {
            item_list[loc].rb.useGravity = true;
        }

        item_list[loc].rb.AddForce(down_stream_float_strength);

        float calc = Mathf.Max(0.33f, (transform.position.y - obj.position.y));

        if (item_list[loc].transition_time < 1.0f)
        {
            item_list[loc].transition_time += Time.deltaTime * (calc);
            if (item_list[loc].transition_time >= 1.0f)
            {
                item_list[loc].transition_time = 1.0f;
            }
        }

        item_list[loc].rb.drag = waterDrag * calc * item_list[loc].transition_time;
        item_list[loc].rb.angularDrag = waterDrag * calc * item_list[loc].transition_time;
    }

}
