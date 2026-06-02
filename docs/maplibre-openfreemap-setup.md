# MapLibre va OpenFreeMap cho Luxe Haven

Luxe Haven hien dung MapLibre GL JS de hien thi ban do vector va OpenFreeMap de cap
style cung tile. Phuong an nay khong can dang ky tai khoan, API key, token hay thong
tin thanh toan.

## Chay local

Tai thu muc goc cua repository:

```powershell
dotnet run --project .\t\t.csproj
```

Khong can them secret hoac bien moi truong cho ban do.

## Kiem tra

- Trang chi tiet can ho hien marker tai toa do da luu.
- Trang dang tin cho phep click ban do hoac keo marker de cap nhat toa do.
- O dia chi tren trang dang tin goi y toi da 5 ket qua tu Photon sau khi nguoi dung
  nhap toi thieu 3 ky tu. Chon goi y se cap nhat marker va toa do.
- Neu mang loi hoac tile provider khong truy cap duoc, giao dien hien thong bao thay
  vi lam hong toan bo trang.

## Gioi han hien tai

- OpenFreeMap cong bo dich vu public khong can API key, khong can dang ky va khong
  gioi han request.
- Dich vu public khong phai SLA danh cho he thong thuong mai lon. Neu luu luong tang
  cao, can chuyen sang tile provider co SLA hoac tu host OpenFreeMap.
- Autocomplete dung public demo server cua Photon. Photon cho phep dung voi luu luong
  hop ly nhung co the throttle va khong cam ket SLA. Neu luu luong tang cao, can tu
  host Photon hoac doi sang provider co SLA.

## Tim can ho gan ban

- Trang danh sach co nut `Gan ban`. Trinh duyet chi xin quyen vi tri sau khi nguoi
  dung bam nut.
- Khi co vi tri, danh sach giu cac bo loc hien tai va sap xep tat ca ket qua phu
  hop tu gan den xa. Khong ap dung gioi han ban kinh.
- Trang chi tiet uu tien de xuat cac can ho gan vi tri dang xem.
- Khoang cach la uoc tinh duong chim bay, khong phai quang duong di chuyen.
- Toa do hien tai duoc gui qua URL cho request dang hoat dong. Ung dung khong luu
  toa do nay vao co so du lieu, cookie hay local storage.

## Tai lieu chinh thuc

- [MapLibre GL JS](https://maplibre.org/maplibre-gl-js/docs/)
- [OpenFreeMap](https://openfreemap.org/)
- [Photon](https://photon.komoot.io/)
- [Photon source va dieu khoan demo server](https://github.com/komoot/photon)
