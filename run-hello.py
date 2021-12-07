# run-hello.py
# 导入Azure ML SDK必要的包
# workspace 对应Azure 机器学习的workspace
# Experiment 对应 Experiment资源
# Environment 对应 Environment
# ScriptRunConfig 为脚本的运行配置必要的参数
from azureml.core import Workspace, Experiment, Environment, ScriptRunConfig

# 确保从workspace下载回来的配置文件`config.js`放在了根目录
ws = Workspace.from_config()
#创建一个新的Experiment
experiment = Experiment(workspace=ws, name='day1-experiment-hello')

#指定脚本的源目录，训练的脚本，以及需要用来训练脚本的机器。需要注意该机器`cpu-cluster`要事先在workspace里创建好
config = ScriptRunConfig(source_directory='./src', script='hello.py', compute_target='Detail-1')

#向workspace 提交训练的任务
run = experiment.submit(config)
#返回该运行的url
aml_url = run.get_portal_url()
print(aml_url)