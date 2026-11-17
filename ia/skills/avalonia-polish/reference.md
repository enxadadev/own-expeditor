# avalonia-polish — referência em XAML

## Tokens (Styles/Tokens.axaml)
```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <CornerRadius x:Key="RadiusSm">4</CornerRadius>
  <CornerRadius x:Key="RadiusMd">8</CornerRadius>
  <CornerRadius x:Key="RadiusLg">12</CornerRadius>
  <x:Double x:Key="SpaceSm">8</x:Double>
  <x:Double x:Key="SpaceMd">12</x:Double>
  <ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key="Light">
      <SolidColorBrush x:Key="SurfaceBrush" Color="#FFFFFF"/>
      <SolidColorBrush x:Key="ImageOutlineBrush" Color="#1A000000"/>
    </ResourceDictionary>
    <ResourceDictionary x:Key="Dark">
      <SolidColorBrush x:Key="SurfaceBrush" Color="#1E1E1E"/>
      <SolidColorBrush x:Key="ImageOutlineBrush" Color="#1AFFFFFF"/>
    </ResourceDictionary>
  </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>
```

## Raio concêntrico
```xml
<Border CornerRadius="{StaticResource RadiusLg}" Padding="4">
  <Border CornerRadius="{StaticResource RadiusMd}"/>
</Border>
```

## Sombras
```xml
<Border BoxShadow="0 1 2 0 #14000000, 0 4 12 0 #0F000000"/>
```

## Transições (botão Fluent)
```xml
<Style Selector="Button /template/ ContentPresenter#PART_ContentPresenter">
  <Setter Property="Transitions">
    <Transitions>
      <BrushTransition Property="Background" Duration="0:0:0.15"/>
      <BrushTransition Property="BorderBrush" Duration="0:0:0.15"/>
    </Transitions>
  </Setter>
</Style>
```

## Pressed
O Fluent já faz `scale(0.98)`. Para desligar num botão específico:
```xml
<Style Selector="Button.no-press:pressed">
  <Setter Property="RenderTransform" Value="none"/>
</Style>
```

## Entrada de painel
```xml
<Style Selector="Border.panel">
  <Setter Property="Transitions">
    <Transitions>
      <DoubleTransition Property="Opacity" Duration="0:0:0.2"/>
      <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.2" Easing="CubicEaseOut"/>
    </Transitions>
  </Setter>
</Style>
<Style Selector="Border.panel:not(.shown)">
  <Setter Property="Opacity" Value="0"/>
  <Setter Property="RenderTransform" Value="translateY(6px)"/>
</Style>
```

## Números
```xml
<TextBlock FontFeatures="+tnum" HorizontalAlignment="Right"
           Text="{Binding DistanceText}"/>  <!-- formatado no ViewModel com CultureInfo atual -->
```

## Texto longo
```xml
<TextBlock Text="{Binding CompanyName}" TextTrimming="CharacterEllipsis"
           ToolTip.Tip="{Binding CompanyName}"/>
```

## Imagem com contorno neutro
```xml
<Border BorderThickness="1" BorderBrush="{DynamicResource ImageOutlineBrush}"
        CornerRadius="{StaticResource RadiusSm}" ClipToBounds="True">
  <Image Source="{Binding Logo}"/>
</Border>
```

## Área de clique em ícone
```xml
<Button MinWidth="40" MinHeight="40" Padding="8" Background="Transparent"
        AutomationProperties.Name="{Binding [Action_Refresh], Source={x:Static loc:Strings.Instance}}">
  <PathIcon Width="16" Height="16" Data="{StaticResource IconRefresh}"/>
</Button>
```
