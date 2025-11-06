1. Supabase 裡面的檢體 Table 是利用 UnitID 來判斷是哪一間醫院的檢體，所以如果這個專案要給其他單位使用，必須要先改 appSetting > DefaultUnitID
2. 


發佈留意
1. 發布的指令：dotnet publish -c Release
2. 發布檔案的位置：bin > Release > publish

現代大甲 Supabase 連線API專案

現代大甲
"DefaultUnitID": "5647b1bb-fd7c-44b6-b57f-b30210231dd6"

現代台南
"DefaultUnitID": "56c493b8-d36d-4ba0-afbb-31f016e12ee1"

杏仁台南
"DefaultUnitID": "c068f09e-fdbc-4739-b97f-df58f92ca813"