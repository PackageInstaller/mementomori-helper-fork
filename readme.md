﻿[[Zh](readme.md)|[En](readme.en.md)]

# MementoMori 游戏助手

施工中 

[反馈和交流](https://t.me/+gTRe8AxKxIdkOTg9)

## **免责声明**

使用本辅助工具的用户（以下简称“用户”）请注意以下几点：

1. 本辅助工具仅供个人娱乐和教育目的使用。使用本工具可能违反特定游戏或应用的使用政策，请自行承担风险。

2. 开发者（以下简称“我们”）不对用户在游戏中使用本工具所产生的任何后果负任何法律或道德责任。用户应自行承担使用工具可能带来的风险。

3. 用户需明白，游戏或应用的开发者可能会采取措施来检测并阻止辅助工具的使用，这可能导致用户的账户被封禁或受到其他制裁。

4. 使用本辅助工具可能违反适用法律。用户需确保自己的使用遵守所有适用法律和法规。

5. 我们保留随时更改或停止本辅助工具的权利，无需提前通知。

通过下载、安装和使用本辅助工具，用户表明已详细阅读并理解此免责声明，并同意自行承担使用本工具所带来的一切风险。如不同意本声明的任何部分，请停止使用本辅助工具。

## 预览
<table>
<tbody>
<tr><td> 

![](images/intro1.png) </td><td>![](images/intro2.png)</td></tr>
<tr><td>

![](images/intro3.png)</td><td>![](images/intro4.png)</td></tr>
<tr><td>

![](images/intro5.png)</td><td>![](images/intro6.png)</td></tr>
<tr><td>

![](images/intro7.png)</td><td>![](images/intro8.png)</td></tr>
</tbody>
</table>


## Todos

<!-- prettier-ignore -->
<table>
  <tbody>
  <tr >
      <td>

- [ ] 多账号支持
- [ ] 主页
    - [x] 用户登陆
    - [x] 领取每日登陆奖励
    - [x] 领取每日 VIP 礼物
    - [x] 一键发送/接收友情点
    - [x] 一键领取礼物箱
    - [x] 一键领取任务奖励
    - [x] 一键领取徽章奖励
    - [x]  一键使用固定物品
    - [x]  月卡奖励自动领取
- [ ] 交换商店
    - [x] 普通物品购买
    - [x] 符石购买
    - [x] 自动购买商品
- [ ] 角色
	- [x] 角色列表
	- [x] 角色属性
	- [x] 角色装备详细
	- [ ] 升级
	- [ ] 突破
    - [ ] 编组

</td>
<td>

- [ ] 储物箱
	- [x] 自动使用物品
	- [x] 自动精炼魔装并继承到D级别装备
	- [x] 神装自动继承到D装
	- [x] 装备自动打磨
	- [ ] 手动使用物品
- [ ] 冒险
    - [x] 领取自动战斗奖励
    - [x] 一键高速战斗
    - [x] 首领一键扫荡
    - [x] 自动刷关
      - [ ] 通过指定关卡后停止
- [ ] 试炼
    - [x] 无穷之塔一键扫荡
    - [x] 无穷之塔自动刷关
      - [ ] 通过指定关卡后停止
    - [ ] 幻影神殿一键挑战
    - [x] 古竞技场一键挑战5次
    - [x] 祈愿之泉一键全部领取
    - [x] 祈愿之泉一键远征
      - [x] 可选仅派遣指定奖励物品的任务
    - [x] 时空洞窟一键自动执行
      - [x] 可选自动购买指定商品
      - [x] 有宝箱节点时优先选择宝箱节点
      - [x] 自动使用恢复道具
</td>
<td>

- [ ] 抽卡
    - [x] 每日免费/金币抽卡
    - [x] 卡池列表
    - [x] 手动抽卡
- [ ] 公会
    - [x] 公会签到
    - [x] 公会讨伐战自动扫荡
    - [x] 自动开启讨伐战
    - [x] 公会战奖励收取
- [ ] 其他
    - [x] 刷关时在支持其他自动任务并行 
    - [x] 刷关配置请求频率限制 

</td>
</tr>

  </tbody>
</table>

## 使用

进入到发布页面：https://github.com/moonheart/mementomori-helper/releases, 然后下载 `publish-win-x64.zip` 解压。

### 方式1 直接运行

要运行的话,你需要配置好你的账号信息. 然后就可以运行 `MementoMori.WebUI.exe` 了, 找到类似 `Now listening on: http://0.0.0.0:5000` 的日志, 打开这个地址就可以了.

### 方式2 用 Docker 运行

```yaml
version: '3'
services:
  mementomori:
    image: moonheartmoon/mementomori-webui:v1
    container_name: mementomori
    restart: unless-stopped
    privileged: false
    ports:
      - "5290:80"
    environment:
      - TZ=Asia/Shanghai
    volumes:
      - ./Master/:/app/Master/
      - ${PWD}/appsettings.user.json:/app/appsettings.user.json:rw
```

- 启动或更新: `docker compose up -d —pull always`
- 停止: `docker compose down`
- 查看日志: `docker compose logs -f`


进入网页之后, 先点击一次登录, 之后就可以随意操作了.

## 自动任务

程序会在特定时间执行特定操作

### 每日任务 (服务器时间 4:10)
- 收取每日登录奖励
- 收取每日 VIP 奖励
- 收取自动战斗奖励
- 收取发送友情点
- 收取礼物箱
- 强化一次装备 (自动选择当前有角色装备的等级最低的装备, 用于完成每日任务) 
- 主线扫荡 3 次
- 无穷之塔扫荡 3 次
- 免费高速战斗 (免费一次, 月卡一次)
- 公会签到
- 公会讨伐战
- 祈愿之泉收取奖励
- 祈愿之前自动派遣
- 时空洞窟自动执行
- 收取每日/每周任务奖励
- 使用固定道具
- 消耗道具抽卡
- 自动进阶角色 (R->R+, SR->SR+)

### 奖励定时收取 (服务器时间 0:30,4:30,8:30,12:30,16:30,20:30)

- 收取每日登录奖励
- 收取自动战斗奖励
- 祈愿之泉派遣+收取
- 公会讨伐
- 友情点收取			
- 任务奖励收取
- 公会战奖励收取
- 自动抽卡
- 使用固定物品

### 竞技场 (服务器时间 20:00)

- 竞技场 5 次 (自动选择列表战力最低作为对手)

## 帐号配置

### 第一步: 获取 UserId 和 ClientKey

#### 方式 1: 有 root 权限 的Android 手机或者模拟器

在 Android 手机上登录一次帐号, 然后找到账号文件 `/data/data/jp.boi.mementomori.android/shared_prefs/jp.boi.mementomori.android.v2.playerprefs.xml`,
在里面搜索:
- UserId: 搜索 `UserId` 找到对应的数字, 这个就是 UserId
- ClientKey: 搜索 `ClientKey` 找到对应的字符串, 将首尾的 `%22` 去掉, 剩下的就是 ClientKey

#### 方式 2: DMM 客户端

在 Windows 上用 DMM 登录一次游戏, 然后找到注册表 `\HKEY_CURRENT_USER\Software\BankOfInnovation\MementoMori`, 拿到 UserId 和 Clientkey
- UserId: xxxxxx_Userid_hxxxxxx
- ClientKey: xxxxxx_ClientKey_hxxxxxx

双击名称, 会显示二进制数据, 把右侧的文本抄下来, ClientKey 共 32 个字母, 注意不要抄错, 不包含引号.

### 第二步: 填写账号到配置文件

新建一个文本文件 `appsettings.user.json`, 填写下面的内容

```json5
{
  "AuthOption": {
    "ClientKey": "", // 在这里填写自己的 clientkey
    "UserId": 0 // 把这里的0改成自己的 Userid
  }
}
```

> 警告! ClientKey 相当于密码, 不要泄露给别人, 不要发到群里.
>
> 警告! ClientKey 相当于密码, 不要泄露给别人, 不要发到群里.
> 
> 警告! ClientKey 相当于密码, 不要泄露给别人, 不要发到群里.

## 常见问题

### 如何多开?

多开需要修改修改端口号, 在 `appsettings.user.json` 里面修改, 比如下面是把端口修改为 5700

```json5
{
  "AuthOption": {
    "ClientKey": "", // 在这里填写自己的clientkey
    "DeviceToken": "",
    "AppVersion": "1.4.4",
    "OSVersion": "Android OS 13 / API-33 (TKQ1.220829.002/V14.0.12.0.TLACNXM)",
    "ModelName": "Xiaomi 2203121C",
    "UserId": 0 // 把这里的0改成自己的用户id
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5700"
      }
    }
  }
}
```
