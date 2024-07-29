![](https://img.itch.zone/aW1nLzE2NDYwNTcyLnBuZw==/original/NTqf4g.png)

# Summary 📝

I played a bunch of Lone Echo and got inspired to re-create the zero-G mechanics from Lone Echo and Echo Arena. Since very few games have them.

I used the development presentation of Ready at Dawn over at GDC vault https://www.gdcvault.com/play/1024446. They had implementation details on the finger procedural animation and other stuff.

# Features ✨

- Procedural IK Hand Animations 🖐️
- Full upper body IK estimation 🔎
- Physically based Zero G Movement 🌌
- 3D Touch screen Unity UI (includes 3D finger position compensation) 📱
- Sample demo level with simple objectives 🎯

# Missing Features ❌

### Thumb IK 🤚

The thumb finger is more complicated than just a hinge joint, so I skipped it for the sake of time.

### Lower Body Simulation 👜

The floating body IK physics animation I kinda skipped since I didn't have enough time for it. But it should be pretty straightforward to implement.

# Implementation Details 💻

Physics interactions differ quite a bit from Lone Echo ones. It comes down to not having low-level access to the physics engine in unity to implement the detailed solution from the GDC presentation. That's why I just use tweaked physics joins to make interactions feel nicer, with some additional tricks to make them more responsive.

The finger IK animation requires intersection calculations on the meshes as described by the GDC presentation. Again we don't have low-level access to the physics engine and the triangle connectivity of physics meshes to help with intersection calculations so I manually calculate them with code ripped from the unity physics package. This probably impacts performance, but I managed to burstify the code, so shouldn't be that much of an issue. I also cache each mesh conectivity.

# Packages Used 📦

- [Simple Unity Audio Manager](https://github.com/jackyyang09/Simple-Unity-Audio-Manager)
- [Unmask For UGUI](https://github.com/mob-sakai/UnmaskForUGUI)
- [Light Probes Volumes](https://github.com/laurenth-personal/LightingTools.LightProbesVolumes)
- [unity-tweens](https://github.com/jeffreylanters/unity-tweens)
- [UnityGLTF](https://github.com/KhronosGroup/UnityGLTF)
- [UI Shapes Kit](https://github.com/thisotherthing/ui-shapes-kit)

# Demo 🎉

You can try the Windows build over at Itch.io [https://simeonradivoev.itch.io/unity-echo](https://simeonradivoev.itch.io/unity-echo)

# Screenshots 📷

![](https://img.itch.zone/aW1hZ2UvMjc1ODMzNS8xNjQ2MDA4OS5wbmc=/original/XVD4OX.png)
![](https://img.itch.zone/aW1hZ2UvMjc1ODMzNS8xNjQ2MDA4Ny5wbmc=/original/oYyahs.png)
![](https://img.itch.zone/aW1hZ2UvMjc1ODMzNS8xNjQ2MDA4Ni5wbmc=/original/SouppL.png)
![](https://img.itch.zone/aW1hZ2UvMjc1ODMzNS8xNjQ2MDA5MC5wbmc=/original/r9%2Finc.png)
![](https://img.itch.zone/aW1hZ2UvMjc1ODMzNS8xNjQ2MDA4OC5wbmc=/original/Qw1Q%2Fw.png)
![](https://img.itch.zone/aW1hZ2UvMjc1ODMzNS8xNjQ2MDA5MS5wbmc=/original/aiV0RC.png)

# Contributing 🤝

I didn't have time to implement all the features. Check out the issues to see what features are missing and need to be implemented or improved. Adding more sounds is always a bonus.

# Credits 🙏

### Music
- [Trailer Music by SondreDrakensson](https://freesound.org/people/SondreDrakensson/sounds/368796/)

### Textures
- [Space Spheremaps by space-spheremaps](https://space-spheremaps.itch.io/space-spheremaps)

### Sounds

- [100 CC0 SFX #2 by rubberduck](https://opengameart.org/content/100-cc0-sfx-2)
- [Magnetic latch cupboard door by wlabarron](https://freesound.org/people/wlabarron/sounds/509112/)
- [small paper notes.flac by breadparticles](https://freesound.org/people/breadparticles/sounds/657829/)
- [Sci Fi Robotic Attachment by simeonradivoev](https://freesound.org/people/simeonradivoev/sounds/740218/)
- [SCI_FI_DOOR by alexo400](https://freesound.org/people/alexo400/sounds/543404/)
- [Bright And Mystery by Lumamorph](https://freesound.org/people/Lumamorph/sounds/669503/)
- [Hi-Tech Button Sound Pack I by Circlerun](https://opengameart.org/content/hi-tech-button-sound-pack-i-non-themed)
- [Spaceship Ambience  by nick121087](https://freesound.org/people/nick121087/sounds/234316/)
- [Propane Torch by DarkSidedGenXer](https://freesound.org/people/DarkSidedGenXer/sounds/654360/)

