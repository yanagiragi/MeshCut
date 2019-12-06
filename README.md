# MeshCut

使用 Unity 的簡易的模型切割

![](https://raw.githubusercontent.com/yanagiragi/MeshCut/master/_img/demo.png)

* 基本原理為對所有的 Mesh 裡面的三角形做 Triangle Plane Intersection, 然後把 intersection point 填補起來

* 填補的方法目前是使用全部 intersection point 連接中心點

  * BLINDED-AM-ME 裡面的方法一效果比較好，尚未實作
  
    * 他的作法把所有 intersection point 鄰近的 edge 都塞到 array 裡面，為鄰近三個點組成一個三角形，然後把生成三角形的新 edge 放到 array 裡面繼續做直到 array 為空。每次會消掉兩個edge但是產生新的edge。

* 目前實作的方法為 loop mesh in subMesh, 因為我把切割面存成另一個 SubMesh，所以想要重複切割一個模型多次會有點問題

# References

* 比較好閱讀，大部分的程式碼都是參考這裡
https://github.com/BLINDED-AM-ME/UnityAssets

* 比較完整的方案
https://github.com/DavidArayan/ezy-slice

* Triangle Plane Intersection 原理的解釋
https://stackoverflow.com/questions/3142469/determining-the-intersection-of-a-triangle-and-a-plane
