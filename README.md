csDelaunay
==========

Fork of the popular C# delaunay library 

**This fork does resource pooling and has Near Zero Garbage = MUCH faster for multiple invocations **

**7.33ms (0.8kb) compared to 27.31ms (2.5MB) without Lloyd**

| Branch | Lloyd Relaxation | GC Alloc | Time ms |
| --------------- | --------------- | --------------- | --------------- |
| PouletFrit/master | 0 | 2.5 MB | 27.31 |
| Joen-UnLogick/removeLRDictionaries | 0 | 1.4 MB | 21.55 |
| Joen-UnLogick/master - First Iteration | 0 | 1.2 MB | 38.08 |
| Joen-UnLogick/master - Second Iteration | 0 | 0.8 KB | 7.33 |
| PouletFrit/master | 4 | 13.0 MB | 134.36  |
| Joen-UnLogick/removeLRDictionaries | 4 | 7.5 MB | 87.16  |
| Joen-UnLogick/master - First Iteration | 4 | 1.3 MB | 92.96 |
| Joen-UnLogick/master - Second Iteration | 4 | 1.1 KB | 55.73 |

As you can see if you only care about first iteration, the removeLRDictionaries branch for which I submitted a pull request is slightly faster. Adding all the allocated data into the pools does take a bit of time.

This is a port and interpretation of actionscript library [as3delaunay](http://nodename.github.io/as3delaunay/)
The original port was made by PouletFrit who also added a Lloyd relaxation function