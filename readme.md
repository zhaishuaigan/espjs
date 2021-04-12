espjs
    一个espruino的命令行开发工具,  支持esp01,eps01s,esp8266开发版等
使用方法:
	1: 将此压缩包里面的文件放到不包含空格和中文的目录中. 如 c:\espruino-tools\
	2: 配置系统环境变量path, 添加 c:\espruino-tools\
	3: 进入项目目录, 在目录的地址栏输入 cmd, 进入cmd程序.
	4: 在命令行输入espjs flash esp01s 烧写espruino固件
	5: 输入espjs upload 上传当前目录中的代码到设备
	然后设备就开始运行代码了, 之后修改代码只需要执行upload 即可更新代码,
	实例可参考 demo 目录下的文件
	index.js 或 main.js 会被当做入口文件加载执行

注意事项:
	1: 端口是自动选择的, 插上设备后会自动选择, 如果有多个设备, 可以使用port命令切换设备
	2: 默认的波特率是115200, 如需修改请到config.json中修改
	3: 如果要增加开发板支持, 请到config.json 的Flash字段追加
	4: 模块不支持远程加载, 例如 require("MQTT") 会提示模块不存在, 解决方法是 手动下载mqtt模块(modules add MQTT), 然后使用 require("modules/MQTT.min.js") 进行引用.
	5: 

详细文档: https://www.kancloud.cn/shuai/espjs