#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
生成16格自动帧贴图的工具脚本
基于现有的48x48单一纹理，生成4x4的帧图集
用于实现Terraria风格的自动连接贴图
"""

import os
from PIL import Image, ImageDraw
import numpy as np

def create_16_frame_tileset(source_image_path, output_path):
    """
    从单一48x48纹理创建16格自动帧贴图 (192x192)
    
    帧布局 (4x4):
    [ 0][ 1][ 2][ 3]  - 0000, 0001, 0010, 0011
    [ 4][ 5][ 6][ 7]  - 0100, 0101, 0110, 0111  
    [ 8][ 9][10][11]  - 1000, 1001, 1010, 1011
    [12][13][14][15]  - 1100, 1101, 1110, 1111
    
    位掩码: 上右下左 (URDL)
    """
    
    # 加载源图像
    if not os.path.exists(source_image_path):
        print(f"源文件不存在: {source_image_path}")
        return False
        
    source = Image.open(source_image_path).convert('RGBA')
    if source.size != (48, 48):
        print(f"源图像尺寸不正确，期望48x48，实际{source.size}")
        return False
    
    # 创建4x4帧图集 (192x192)
    tileset = Image.new('RGBA', (192, 192), (0, 0, 0, 0))
    
    # 为每个帧生成连接变体
    for frame_id in range(16):
        # 解析位掩码 (上右下左)
        has_up = bool(frame_id & 1)
        has_right = bool(frame_id & 2)
        has_down = bool(frame_id & 4)
        has_left = bool(frame_id & 8)
        
        # 创建当前帧
        frame = create_connected_frame(source, has_up, has_right, has_down, has_left)
        
        # 计算在图集中的位置
        grid_x = frame_id % 4
        grid_y = frame_id // 4
        
        # 粘贴到图集中
        tileset.paste(frame, (grid_x * 48, grid_y * 48))
    
    # 保存图集
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    tileset.save(output_path)
    print(f"生成16格帧图集: {output_path}")
    return True

def create_connected_frame(source, has_up, has_right, has_down, has_left):
    """
    根据连接情况创建帧变体
    使用边缘混合技术来实现无缝连接
    """
    frame = source.copy()
    
    # 边缘处理区域 (像素)
    edge_size = 6
    
    # 如果某个方向有连接，则该边缘使用内部纹理扩展
    # 如果没有连接，则保持原始边缘
    
    if has_up:
        # 上边缘：使用内部纹理向上扩展
        extend_edge(frame, 'top', edge_size)
    
    if has_right:
        # 右边缘：使用内部纹理向右扩展
        extend_edge(frame, 'right', edge_size)
    
    if has_down:
        # 下边缘：使用内部纹理向下扩展
        extend_edge(frame, 'bottom', edge_size)
    
    if has_left:
        # 左边缘：使用内部纹理向左扩展
        extend_edge(frame, 'left', edge_size)
    
    return frame

def extend_edge(image, direction, edge_size):
    """
    扩展图像边缘以实现无缝连接
    """
    width, height = image.size
    pixels = np.array(image)
    
    if direction == 'top':
        # 使用第edge_size行的像素填充前edge_size行
        source_row = pixels[edge_size]
        for i in range(edge_size):
            pixels[i] = source_row
    
    elif direction == 'bottom':
        # 使用倒数第edge_size行的像素填充后edge_size行
        source_row = pixels[height - edge_size - 1]
        for i in range(height - edge_size, height):
            pixels[i] = source_row
    
    elif direction == 'left':
        # 使用第edge_size列的像素填充前edge_size列
        source_col = pixels[:, edge_size]
        for i in range(edge_size):
            pixels[:, i] = source_col
    
    elif direction == 'right':
        # 使用倒数第edge_size列的像素填充后edge_size列
        source_col = pixels[:, width - edge_size - 1]
        for i in range(width - edge_size, width):
            pixels[:, i] = source_col
    
    # 更新图像
    updated_image = Image.fromarray(pixels, 'RGBA')
    image.paste(updated_image)

def process_all_tiles():
    """
    处理所有瓦片纹理
    """
    # 定义要处理的瓦片
    tiles_to_process = [
        ('Content/Tiles/Dirt_Block_(placed).png', 'Content/Tiles/Dirt_Block_Frames.png'),
        ('Content/Tiles/Stone_Block_(placed).png', 'Content/Tiles/Stone_Block_Frames.png'),
        ('Content/Tiles/Sand_Block_(placed).png', 'Content/Tiles/Sand_Block_Frames.png'),
        ('Content/Tiles/Snow_Block_(placed).png', 'Content/Tiles/Snow_Block_Frames.png'),
        ('Content/Tiles/Marble_Block_(placed).png', 'Content/Tiles/Marble_Block_Frames.png'),
    ]
    
    success_count = 0
    
    for source_path, output_path in tiles_to_process:
        if create_16_frame_tileset(source_path, output_path):
            success_count += 1
        else:
            print(f"处理失败: {source_path}")
    
    print(f"\n处理完成: {success_count}/{len(tiles_to_process)} 个文件")
    print("\n使用说明:")
    print("1. 生成的帧图集为192x192像素 (4x4 * 48x48)")
    print("2. 在C#代码中使用TileFrameProcessor.GetFrameSourceRect()获取正确的帧")
    print("3. 帧ID对应4位二进制掩码 (上右下左)")

if __name__ == '__main__':
    print("16格自动帧贴图生成器")
    print("=" * 40)
    process_all_tiles()