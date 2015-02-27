using FiledRecipes.Domain;
using FiledRecipes.App.Mvp;
using FiledRecipes.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FiledRecipes.Views
{
    /// <summary>
    /// 
    /// </summary>
    public class RecipeView : ViewBase, IRecipeView
    {

        public void Show(IRecipe recipe)
        {
            Console.Clear();
            Header = recipe.Name;
            ShowHeaderPanel();
            int instructionValue = 1;

            Console.WriteLine();
            Console.WriteLine("Ingredienser");
            Console.WriteLine("============");
            foreach (IIngredient i in recipe.Ingredients)
            {
                Console.WriteLine(i);
            }

            Console.WriteLine();
            Console.WriteLine("Gör så här");
            Console.WriteLine("==========");
            foreach (string s in recipe.Instructions)
            {
                Console.WriteLine(string.Format("<{0}>", instructionValue));
                Console.WriteLine(s);
                instructionValue++;
            }
            
        }
        public void Show(IEnumerable<IRecipe> recipes)
        {
            foreach (IRecipe r in recipes)
            {
                Show(r);
                ContinueOnKeyPressed();
            }
        }
    }
}
