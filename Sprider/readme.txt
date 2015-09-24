1. 爬到一半时一直request 超时
//此处GetResponse超过的原因是，当前存在太多数目的alive的http连接（大于10个），所以再次提交同样的http的request，再去GetResponse，就会超时死掉。
//解决办法就是，把DefaultConnectionLimit 设置为一个比较大一点的数值，此数值保证大于你当前已经存在的alive的http连接数即可。
System.Net.ServicePointManager.DefaultConnectionLimit = 50;

2.无法爬去 www.djhealthunion.com\campus
是因为  baseUri = new Uri(url); 改变 ，而不是最初的 www.djhealthunion.com

3.CSS里的图片不能能挡取
   正则匹配css 里的图片
  //相对路径转换为绝对路径【很神奇的函数】
  baseUri= www.djhealthunion.com\css
  url=../images/line-b.jpg
  Uri uri = new Uri(baseUri, url);-----》www.djhealthunion.com/images/line-b.jpg

4.ico 图标文件不完整，gif图片不完整

保存图片的代码只读取了第一帧，后面帧没能正常读取。修改保存图片的代码，一块一块写入
------------------------------------------------------------------------------------------
缺陷:
不能够方便的配置，效率有些低


