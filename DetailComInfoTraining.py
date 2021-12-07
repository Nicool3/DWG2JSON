import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import os
import json
import unicodedata
import warnings
import random
import math
from keras.utils import to_categorical
from keras import models
from keras import layers
warnings.filterwarnings("ignore")

def load_json(path):
    file = open(path, "r", encoding='utf-8-sig')
    text = file.read()
    return pd.json_normalize(json.loads(text))

def increase_data(data_raw, n_scale = 60, n_test = 0):
    data_increased = pd.DataFrame(columns=['Name','Center', 'Length','Width','Scale', 'LineInfoList', 'CircleInfoList', 'TextInfoList'])
    for i in range(data_raw.shape[0]-n_test):
        name = data_raw.iloc[i]['Name']
        center = data_raw.iloc[i]['Center']
        length = data_raw.iloc[i]['Length']
        width = data_raw.iloc[i]['Width']
        scale = data_raw.iloc[i]['Scale']
        for j in range(n_scale):
            line_list = data_raw.iloc[i]['LineInfoList'][:]
            circle_list = data_raw.iloc[i]['CircleInfoList'][:]
            text_list = data_raw.iloc[i]['TextInfoList'][:]
            random.shuffle(line_list)
            random.shuffle(circle_list)
            random.shuffle(text_list)
            new_info = pd.Series({'Name':name, 'Center':center, 'Length':length, 'Width':width, 'Scale':scale,
                                'LineInfoList':line_list, 'CircleInfoList':circle_list, 'TextInfoList':text_list})
            data_increased = data_increased.append(new_info, ignore_index=True)
    return data_increased

# 将图素信息转化为tokens
def info_to_tokens(info):
    tokens = []
    scale = info['Scale']
    for line in info['LineInfoList']:
        x = round(line['X'] / scale,3)
        y = round(line['Y'] / scale,3)
        length = line['Length'] / scale
        sin_angle = line['SinAngle']
        cos_angle = math.sqrt(1-sin_angle*sin_angle)
        x2 = round(x+length * cos_angle,3)
        y2 = round(y+length * sin_angle,3)
        tokens.append((1,x,y,x2,y2))
    for circle in info['CircleInfoList']:
        x = round(circle['X'] / scale,3)
        y = round(circle['Y'] / scale,3)
        r = round(circle['Radius'] / scale,3)
        tokens.append((2,x,y,r,r))
    for text in info['TextInfoList']:
        x = round(text['X'] / scale,3)
        y = round(text['Y'] / scale,3)
        string = text['TextString']
        n = len(string)
        rein_flag = 1 if '%%13' in string or '@' in string else 0
        tokens.append((3,x,y,n,rein_flag))
    return tokens

# 将列表每一项处理为长度一致项
def fill_tokens(tokens, n_token):
    if(len(tokens)>=n_token):
        return np.array(tokens[:n_token])
    else:
        length = len(tokens)
        tokens.extend([(0,0,0,0,0) for i in range(n_token-length)])
        return np.array(tokens)

def model_training():

    print("Loading json file......")
    data_raw = load_json("DetailComInfoTry.json")

    print("Increasing data by shuffle info records......")
    data_increased = increase_data(data_raw)

    print("Preparation for data to learn......")
    data_list = []
    for index in range(data_increased.shape[0]):
        data_list.append(info_to_tokens(data_increased.iloc[index]))
    # 最长的序列长度
    n_token = sorted([len(x) for x in data_list], reverse=True)[0]
    filled_data_list = []
    for index in range(len(data_list)):
        filled_data_list.append(fill_tokens(data_list[index], n_token))
    X = np.array(filled_data_list)

    cat_list=[name for name in data_increased['Name']]
    label_dict = {string:i for (i, string) in enumerate(list(set(cat_list)))}
    label_index_dict ={v:k for k,v in label_dict.items()}
    num_labels = [label_dict[name] for name in cat_list]
    y = to_categorical(num_labels)

    dict1 = {'n_token':n_token,
             'label_dict':label_dict,
             'label_index_dict': label_index_dict}
    with open("DetailComInfoPara.json","w") as f:
        json.dump(dict1,f)

    print("Training......")
    model = models.Sequential()
    model.add(layers.Dense(16, activation='sigmoid', input_shape=(n_token,5,)))
    model.add(layers.Dense(8, activation='relu'))
    model.add(layers.Flatten())
    model.add(layers.Dense(4, activation='softmax'))
    model.compile(optimizer='rmsprop',loss='categorical_crossentropy',metrics=['accuracy'])
    history = model.fit(X,y,epochs=20,batch_size=32,validation_split=0.2)

    save_path = "DetailComInfoModel.h5"
    model.save(save_path)

if __name__ == '__main__':
    model_training()