global using System.IO;
global using System.Text;
global using System.ComponentModel;
global using System.Runtime.CompilerServices;
global using System.Windows;

global using ConquerMapViewer.Core.Domain.Entities;
global using ConquerMapViewer.Core.Interfaces;
global using ConquerMapViewer.Core.Domain.ValueObjects;
global using ConquerMapViewer.Infrastructure.Animation;
global using ConquerMapViewer.Rendering.Primitives;
global using ConquerMapViewer.Infrastructure.FileLoaders;
global using ConquerMapViewer.Infrastructure.FileSystem;
global using ConquerMapViewer.Infrastructure.Repositories;
global using ConquerMapViewer.WPF.ViewModels;
global using ConquerMapViewer.WPF.Views;
global using ConquerMapViewer.WPF.DependencyInjection;
global using ConquerMapViewer.Core.Domain.Enums;
global using ConquerMapViewer.Core.Services;
global using ConquerMapViewer.Rendering.Coordinates;
global using ConquerMapViewer.Rendering.Drawing;

global using Microsoft.Xna.Framework;
global using SevenZipExtractor;
global using MonoGame.Framework.WpfInterop;
global using MonoGame.Framework.WpfInterop.Input;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Xna.Framework.Graphics;


global using Point = Microsoft.Xna.Framework.Point;