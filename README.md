# Unity Engine Extension

`com.lukeandrews.unity-extension` · Unity 6000.0 or later · MIT

Systems and helpers that Unity doesn't provide: a managed update loop, timers, code-driven animation, object pools, a
web API client, and serializable types the editor can't otherwise show.

## Install

List the package as a git dependency in `Packages/manifest.json`:

```json
"com.lukeandrews.unity-extension": "https://github.com/Sunshine-Kittens/Unity-Extensions.git",
```

or add the repository as a git submodule at `Packages/com.lukeandrews.unity-extension`, which Unity loads as an
embedded package. Pin a commit for a live project. The package is the repository root, so don't add a `?path=`.

It depends on Newtonsoft Json (`com.unity.nuget.newtonsoft-json` 3.2.1) and Addressables (`com.unity.addressables`
2.7.4), which the Package Manager resolves from Unity's registry.

## Systems

### Managed update

`UpdateManager` calls registered objects once per frame from its own player-loop systems, which run just before
Unity's `Update`, `LateUpdate` and `FixedUpdate`.

```csharp
public sealed class Spinner : MonoBehaviour, IUpdatable
{
    public bool Active => isActiveAndEnabled;

    private void OnEnable() => UpdateManager.AddUpdatable(this);
    private void OnDisable() => UpdateManager.RemoveUpdatable(this);

    public void ManagedUpdate() => transform.Rotate(0f, 90f * Time.deltaTime, 0f);
}
```

- Implement `IUpdatable`, `ILateUpdatable` or `IFixedUpdatable`. Each extends `IManagedObject`, whose `Active` decides
  whether the object is called this frame.
- Nothing is removed when an object is destroyed, so register in `OnEnable` and remove in `OnDisable`. Any object can
  register, not only a MonoBehaviour.
- `AddStaticUpdatable` and its siblings take a static method instead.
- Each call is wrapped so one exception is logged without stopping the rest. Call order isn't stable.

### Player loop

`PlayerLoop` wraps `UnityEngine.LowLevel.PlayerLoop`: `InsertSystem`, `InsertSystemBefore<T>`, `InsertSystemAfter<T>`,
`ReplaceSystem<T>` and `TryRemoveSystem` add or remove an `IPlayerLoopSystem`. The package inserts the timer and
managed-update systems itself after assemblies load. On quit it restores Unity's default loop, which also removes
systems other code inserted.

### Timers

```csharp
private TimerHandle _hint;

TimerManager.SetTimer(ref _hint, ShowHint, 5f);
TimerManager.ClearTimer(_hint);
```

`SetTimer(ref handle, callback, rate, loop = false, initialDelay = -1, unscaledTime = true)` calls the callback after
`initialDelay` seconds if it is set, otherwise after `rate`, and then every `rate` seconds when looping. Timers use
unscaled time unless told otherwise. `PauseTimer`,
`UnPauseTimer`, `SetTimerForNextTick` and `ClearAllTimers` complete the set. Timers run in their own player-loop system
before `Update`.

### Animation

`AnimationPlayer` plays an `IAnimation` (a `Length`, `Prepare` and `Evaluate(normalisedTime)`) and is advanced in the
managed `LateUpdate`. It supports `Play`, `Pause`, `Resume`, `Rewind`, `Stop`, `SetCurrentTime` and `Complete`, an
`OnComplete` callback, playback speed, `PlaybackMode` (`Forward` or `Reverse`), `TimeMode` (`Scaled` or `Unscaled`)
and 23 easing curves in `EasingMode`.

### Object pools

- `ComponentObjectPool<T>`: serializable, built from a template component. `Get` returns an instance, `Prewarm`
  creates some ahead of time, and `ReturnToPool` parks one.
- `AddressableGameObjectPool` (by address) and `AssetReferenceGameObjectPool` (by `AssetReferenceGameObject`): load the
  asset once, return instances from an async `Get`, and release the asset when the pool empties.
- A component on the pooled prefab can implement `IPooledObjectListener` to hear when its object is acquired,
  returned or destroyed.
- An instance destroyed by other code leaves its pool on its own.
- Pools can cap their parked instances (`MaxParked`), keep their parking root across scenes (`PersistPoolRoot`), and
  `Trim`, `Prune` or `Clear`. `InstantiatedCount`, `ReusedCount` and `PeakActiveCount` report usage.

### Web API

- Transport (`UnityEngine.Extension.WebAPI` and `UnityEngine.Extension.WebAPI.Http`): `HttpClient.Instance` sends an
  `HttpRequest` and returns an `IHttpResponse`. It defaults to `UnityWebRequestHttpClient`, which throws
  `HttpTransportException` on a connection error or a non-2xx status. Call `HttpClient.Initialize` once, before first
  use, to replace it.
- Helpers: `QueryBuilder`, `UrlPath.Escape`, `MultipartFormSection`, header and JSON-body extensions on requests, and
  status and body readers on responses.
- Services: `WebServiceProvider<T>`, `WebServiceRequest<TProvider, TResponse>` and `JsonWebServiceResponse<T>` build
  typed calls against a `WebServiceEnvironment`, with an optional `IRequestAuthenticator`.
- `ServiceProvider<TSelf, TConfiguration>`: a lazily created static `Instance` whose configuration is chosen by
  environment name, with `SetEnvironment` and `OnEnvironmentChanged`.

## Types

| Type | What it is |
|---|---|
| `InterfaceReference<T>` | A serialized reference to a Unity object by an interface it implements. The drawer also accepts a GameObject that has a component implementing it. |
| `SubclassOf<T>` | Picks a public, non-abstract subclass of class `T` in the inspector; `CreateInstance(args)` constructs it. It saves the type's assembly-qualified name, so renaming or moving the subclass clears the field. |
| `SerializedDictionary` | A `Dictionary` that serializes, with ready-made string, int and Unity-object versions and inspector drawers. Duplicate or null keys in saved data throw on load. |
| `PriorityCollection<T>` | Unique elements iterated highest priority first, ties in the order added. |
| `ScalarFlag` | A counted flag: each `true` adds one and each `false` takes one away, and it reads true while the count is above zero. `OnUpdate` fires when the value changes. |
| `ObjectTypeMap<T>` | Looks objects up by their runtime type; two of the same type throw. |
| `Singleton<T>` | An abstract MonoBehaviour singleton. `Instance` is set in `Awake`, so it is null before then. Implement `Persists`; a persistent singleton must sit at the scene root. A second instance throws. Overrides of `Awake` and `OnDestroy` must call the base. |

## Extension methods

- Collections: `IsValidIndex`, `IncrementIndex`, `DecrementIndex` and `Shuffle` on lists, `RandomElement` on lists and
  arrays, `Split` on `IList<T>`.
- `Vector2.Rotate(degrees)`, `float.Remap(…)`, and `DebugExtension` and `GizmoExtension` for drawing squares and boxes.
- `await` on an `AsyncOperation`, from the namespace `UnityEngine.Extension.Awaitable`. Inside a `UnityEngine.Extension`
  namespace that name hides `UnityEngine.Awaitable`, so write the latter out in full there.

## Static state

`UpdateManager`, the animation runner, `ServiceProvider.Instance` and `HttpClient.Instance` are static and aren't
reset when play mode starts. With domain reload turned off (Enter Play Mode Options), what one play session registered
is still there in the next. `TimerManager` resets itself.

## Assemblies and tests

| Assembly | Platforms | Namespaces |
|---|---|---|
| `UnityEngine.Extension` | All | `UnityEngine.Extension`, `.Awaitable`, `.SerializedDictionary`, `.WebAPI`, `.WebAPI.Http` |
| `UnityEditor.Extension` | Editor | `UnityEditor.Extension`, `UnityEditor.Extension.SerializedDictionary` |
| `UnityEngine.Extension.Tests` | Editor, tests only | |
| `UnityEngine.Extension.PlayTests` | Play mode, tests only | |

The tests cover the object pools only: 39 edit-mode tests and 17 play-mode tests.
