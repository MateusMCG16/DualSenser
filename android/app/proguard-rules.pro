# Regras Proguard para DualSenser
-keepclassmembers class * {
    @kotlinx.serialization.Serializable <fields>;
}
-keepattributes *Annotation*,Signature,InnerClasses,EnclosingMethod
