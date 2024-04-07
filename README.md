# Trade Recorder (原 Trade Buddy)
[![](https://img.shields.io/badge/dynamic/xml?color=success&label=repo%20version&query=%2F%2FProject%2FPropertyGroup%2FVersion&url=https%3A%2F%2Fraw.githubusercontent.com%2Fstatus102%2FTradeRecorder%2Fmaster%2FTradeRecorder%2FTradeRecorder.csproj)](https://github.com/status102/TradeRecorder/raw/master/latest.zip)
[![](https://img.shields.io/github/v/release/status102/TradeRecorder.svg)](https://github.com/status102/TradeRecorder/releases/latest)

记录交易的内容并根据预先设定计算交易金额，不会自动填写进交易窗口。

Record the content of the transaction and calculate the transaction amount according to the preset, and will not automatically fill in the transaction window.

如果有什么新点子或者什么问题，欢迎提出。

If you have any idea or find any problem, please make an issue or connect with me in [discord](https://discord.gg/5bzGGyazEC).

***注意***

程序更名后，配置信息不支持自动导入，且设置窗口使用的导入导出格式有所差异，故不支持旧版本数据的直接导入。

## 安装说明

1. 在<Dalamud设置>-<自定义插件仓库>中添加`https://github.com/status102/TradeRecorder/raw/master/repo.json`
2. 安装Trade Recorder

***Notice***

- 如果你使用的游戏客户端不是CN服务器的6.25版本，请在设置页面中更新Opcode后再使用！
- If the game client you are using is **NOT** version 6.25 of the CN Server, please update Opcode in the setting page before using it!

## 使用方法

- `/tr` 打开交易历史记录窗口，查看过往交易
- `/tr` open history window

![](https://github.com/status102/TradeRecorder/raw/master/Image/History.png)

- `/tr cfg|config` 打开设置窗口设置物品的预期价格
- `/tr cfg|config` open setting window

![](https://github.com/status102/TradeRecorder/raw/master/Image/Setting.png)

## Opcode更新

1. 勾选<修改Opcode>选项
2. 点击按钮1<捕获Opcode>，然后在1分钟内与任意玩家申请交易，并取消
3. 点击按钮2<从GitHub更新以下Opcode>，程序将会自动从GitHub的 [仓库](https://github.com/karashiiro/FFXIVOpcodes) 获取Opcode。如果客户端语言为简中则使用CN的Opcode，其他语言则使用Global。

![](https://github.com/status102/TradeRecorder/raw/master/Image/ChangeOpcode.png)

## Update Opcode

1. Check off <修改Opcode>
2. Click button 1, then trade with other and cancel in 1 min
3. Click button 2, program will automatically get the Opcode from the [GitHub repository](https://github.com/karashiiro/FFXIVOpcodes). If ClientLanguage is ChineseSimplified, use the Opcode of CN, and use Global for other languages.

![](https://github.com/status102/TradeRecorder/raw/master/Image/ChangeOpcode.png)

## Todo

- [x] 交易时即时查询交易物品价格

- [x] 对同一目标多次交易时，累积记录多次交易内容

- [x] 交易历史增加物品图标显示

- [x] 更换为通过opcode或者sig获取交易itemId

- [x] 点击输出信息的角色名查看该角色交易记录

- [ ] 右键查看该角色交易记录

- [ ] 历史记录更多种筛选

## Thanks

感谢獭爹、Loskh、zaevi、Yui以及幻想科技群群友

## 附

第一个稍微正经一点的项目，试着学习使用Git、版本控制、各种规范以及新玩意，依旧会很丑陋就是了
